using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Infrastructure.Fx;
using CrossLedgerWeb.Infrastructure.Identity;
using CrossLedgerWeb.Infrastructure.Persistence;
using CrossLedgerWeb.Infrastructure.Reporting;
using CrossLedgerWeb.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrossLedgerWeb.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CrossLedgerWebDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IFxSettlementWalletResolver, FxSettlementWalletResolver>();
        services.AddScoped<IPayoutReserveWalletResolver, PayoutReserveWalletResolver>();
        services.AddScoped<IPayoutRepository, PayoutRepository>();
        services.AddScoped<IProcessedWebhookEventStore, ProcessedWebhookEventStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IRoutingAuditLog, RoutingAuditLog>();
        services.AddScoped<IProviderStatsProvider, DefaultProviderStatsProvider>();
        services.AddScoped<ITransactionHistoryReader, TransactionHistoryReader>();
        services.AddSingleton<IClock, SystemClock>();

        // CrossLedgerWebDbContext needs IDataProtectionProvider to encrypt TOTP secrets at
        // rest (specification 6.4). Locally this persists keys to the file system; in
        // production it would be configured with PersistKeysToAzureKeyVault.
        services.AddDataProtection();

        AddExchangeRateProviders(services, configuration);
        AddIdentityAndJwt(services, configuration);

        return services;
    }

    private static void AddIdentityAndJwt(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // Argon2PasswordHasher below replaces Identity's default PBKDF2 hasher;
                // these options are the account-policy side of specification 6.1.
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<CrossLedgerWebDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IPasswordHasher<ApplicationUser>, Argon2PasswordHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IStepUpTokenValidator, StepUpTokenValidator>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<ITotpProvider, TotpProvider>();
        services.AddScoped<ITwoFactorCredentialRepository, TwoFactorCredentialRepository>();
        services.AddScoped<IRecoveryCodeRepository, RecoveryCodeRepository>();
        services.AddScoped<IUsedTotpCodeRepository, UsedTotpCodeRepository>();
    }

    private static void AddExchangeRateProviders(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExchangeRateApiOptions>(configuration.GetSection(ExchangeRateApiOptions.SectionName));

        services.AddHttpClient<ExchangeRateApiProvider>(client =>
        {
            client.BaseAddress = new Uri("https://v6.exchangerate-api.com/");
        });

        services.AddHttpClient<FrankfurterProvider>(client =>
        {
            // frankfurter.app now redirects here permanently; pointing at the current
            // domain directly avoids paying for that redirect on every call. Frankfurter
            // is ECB-sourced, so it only covers the ~30 currencies the ECB publishes -
            // PKR is notably not one of them, so it cannot actually fall back for the
            // USD -> PKR corridor the specification uses as its own example. It still
            // covers the major/EU corridors it was chosen for.
            client.BaseAddress = new Uri("https://api.frankfurter.dev/v1/");
        });

        services.AddMemoryCache();

        // Composition order: Caching(Resilient(Primary, Fallback)) - a singleton so the
        // circuit breaker inside ResilientExchangeRateProvider actually accumulates
        // state across requests instead of resetting every call.
        services.AddSingleton<IExchangeRateProvider>(sp =>
        {
            var primary = sp.GetRequiredService<ExchangeRateApiProvider>();
            var fallback = sp.GetRequiredService<FrankfurterProvider>();
            var resilient = new ResilientExchangeRateProvider(primary, fallback);
            var cache = sp.GetRequiredService<IMemoryCache>();
            return new CachingExchangeRateProvider(resilient, cache);
        });
    }
}
