// Azure environment for CrossLedgerWeb (specification section 10, week 11 "Cloud deploy").
// Authored and syntax-validated (`az bicep build --file infra/main.bicep`) but never
// deployed from this repository - that needs the maintainer's own Azure subscription and
// is their call, not something to run automatically.
//
// Provisions only what the system actually needs to run what is already built:
//   - App Service Plan + App Service, hosting CrossLedgerWeb.Api
//   - Static Web App, hosting the Blazor WebAssembly frontend
//   - Azure SQL Server + Database, replacing LocalDB/the docker-compose sqlserver
//     container for a cloud deployment
//   - Key Vault, with the App Service's system-assigned managed identity granted the
//     "Key Vault Secrets User" role - integrates codelessly via App Service "Key Vault
//     references" (an app setting shaped @Microsoft.KeyVault(SecretUri=...)), so this
//     needs zero C# changes to start using
//   - Log Analytics Workspace + Application Insights - also codeless via the
//     APPLICATIONINSIGHTS_CONNECTION_STRING app setting and the App Service "Application
//     Insights" extension. Richer code-level telemetry (the
//     Microsoft.ApplicationInsights.AspNetCore package) would be a genuine follow-up, not
//     something this template should be mistaken for already providing.
//
// Deliberately NOT provisioned:
//   - Azure Cache for Redis - confirmed zero references to IDistributedCache or
//     StackExchange.Redis anywhere in src/. The docker-compose redis container is already
//     dead weight nothing connects to; carrying that into "real" cloud IaC would be the
//     same fake-for-show-resource problem avoided below for Functions and Service Bus.
//   - Azure Functions - no Functions-shaped workload exists in the code; the card
//     authorization expiry sweeper runs as an in-process BackgroundService.
//   - Service Bus - no messaging code exists anywhere in the codebase ("Microservices
//     concepts" is an aspirational CV-mapping line, not a built feature).

@description('Short name used to derive resource names, e.g. "dev", "prod".')
param environmentName string

@description('Azure region for every resource.')
param location string = resourceGroup().location

@description('SQL Server administrator login.')
@secure()
param sqlAdminLogin string

@description('SQL Server administrator password.')
@secure()
param sqlAdminPassword string

var namePrefix = 'crossledger-${environmentName}'
var sqlServerName = '${namePrefix}-sql'
var sqlDatabaseName = 'CrossLedgerDatabase'
var appServicePlanName = '${namePrefix}-plan'
var apiAppName = '${namePrefix}-api'
var staticWebAppName = '${namePrefix}-web'
var keyVaultName = take('${namePrefix}-kv', 24)
var logAnalyticsName = '${namePrefix}-logs'
var appInsightsName = '${namePrefix}-insights'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource sqlAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'ConnectionStrings__Default'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=SqlConnectionString)'
        }
        {
          // appsettings.json ships an empty placeholder ("") for this on purpose - dev
          // reads the real value from `dotnet user-secrets`. A real deploy from this
          // template must not inherit that empty string: an empty HMAC signing key
          // collapses JWT forging resistance to nothing. The secret itself (named
          // "JwtSigningKey") is never set by this template - it's generated once and
          // uploaded to the vault out-of-band, the same way SqlConnectionString is.
          name: 'Jwt__SigningKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=JwtSigningKey)'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

// "Key Vault Secrets User" - lets the API's managed identity read secrets (connection
// strings, provider API keys) without ever holding a vault access policy or a static key.
resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}

resource apiAppKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiApp.id, keyVaultSecretsUserRole.id)
  scope: keyVault
  properties: {
    roleDefinitionId: keyVaultSecretsUserRole.id
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      appLocation: '/'
      outputLocation: 'wwwroot'
    }
  }
}

output apiHostname string = apiApp.properties.defaultHostName
output staticWebAppHostname string = staticWebApp.properties.defaultHostname
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output keyVaultUri string = keyVault.properties.vaultUri
output appInsightsConnectionString string = appInsights.properties.ConnectionString
