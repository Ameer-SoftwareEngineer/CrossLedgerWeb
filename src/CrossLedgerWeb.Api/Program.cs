using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using CrossLedgerWeb.Api.ExceptionHandling;
using CrossLedgerWeb.Api.RealTime;
using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application;
using CrossLedgerWeb.Infrastructure;
using CrossLedgerWeb.Infrastructure.Identity;
using CrossLedgerWeb.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Lets Swagger UI attach a "Bearer {token}" header to try-it-out calls against
    // endpoints behind [Authorize] once step-up/RBAC land on top of this.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token from POST /api/v1/auth/login.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPaymentProviders(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddHostedService<FxRateBroadcastService>();

// CrossLedgerFrontend runs on its own origin (a separate repo, a separate dev server
// port) - without this, the browser blocks every call the Blazor WASM app makes here.
// AllowCredentials is here for the SignalR hub specifically - its client negotiates with
// credentials included, which the CORS spec refuses to combine with a wildcard origin, so
// this policy has always needed WithOrigins over AllowAnyOrigin regardless.
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim types exactly as JwtTokenGenerator wrote them (e.g. "sub") instead
        // of the framework's default inbound-claim renaming, so HttpContext.User reads
        // consistently with StepUpTokenValidator and IJwtTokenGenerator's own claims.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? string.Empty)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Browsers can't attach an Authorization header to a WebSocket handshake, so the
        // SignalR client instead puts the access token on the query string - this is the
        // one place that's accepted from, and only for the hub path, so it can't be used
        // to bypass the header requirement on ordinary API calls.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// Specification 6.4: "Rate limiting on verification" - keyed per authenticated user
// (falling back to IP if that's ever missing) so one account's lockout can't be used to
// deny another user's attempts, and queueing is off so an exhausted caller gets an
// immediate 429 rather than piling up requests.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimiterPolicies.TotpVerification, httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.MapHub<FxRateHub>("/hubs/fx-rates");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .ExcludeFromDescription();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await IdentityRoleSeeder.SeedRolesAsync(roleManager);

    // Nothing else in this codebase ever grants Admin/Support - every self-registration
    // hardcodes Customer - so without this the Admin Console (specification 9) would have
    // no way to be exercised locally short of a manual database edit.
    if (app.Environment.IsDevelopment())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DevelopmentAdminSeeder.SeedAsync(userManager);
    }
}

app.Run();

// Exposed for CrossLedgerWeb.Integration.Tests' WebApplicationFactory<Program> (specification 3.6).
public partial class Program;
