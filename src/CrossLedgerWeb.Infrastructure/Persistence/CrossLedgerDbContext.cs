using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.Wallets;
using CrossLedgerWeb.Infrastructure.Identity;
using CrossLedgerWeb.Infrastructure.Persistence.Configurations;
using CrossLedgerWeb.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Persistence;

/// <summary>Inherits IdentityDbContext rather than composing a separate identity store,
/// so user/role/token tables and the ledger's own tables share one migration history
/// and one SaveChanges transaction boundary.</summary>
public sealed class CrossLedgerWebDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly IDataProtector _totpSecretProtector;
    private readonly IDataProtector _cardDataProtector;

    public CrossLedgerWebDbContext(DbContextOptions<CrossLedgerWebDbContext> options, IDataProtectionProvider dataProtectionProvider)
        : base(options)
    {
        _totpSecretProtector = dataProtectionProvider.CreateProtector("CrossLedgerWeb.TwoFactorCredential.Secret");
        _cardDataProtector = dataProtectionProvider.CreateProtector("CrossLedgerWeb.Card.PanAndCvv");
    }

    // Only aggregate roots get a DbSet - LedgerEntry is reached exclusively through
    // Wallet.Entries, matching the repository pattern's aggregate boundary.
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<OtcRequest> OtcRequests => Set<OtcRequest>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<RoutingDecisionRecord> RoutingDecisions => Set<RoutingDecisionRecord>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TwoFactorCredential> TwoFactorCredentials => Set<TwoFactorCredential>();
    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();
    public DbSet<UsedTotpCode> UsedTotpCodes => Set<UsedTotpCode>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardPurchase> CardPurchases => Set<CardPurchase>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TwoFactorCredentialConfiguration needs an IDataProtector that only this
        // context (constructed via DI) can supply, so it's excluded from the assembly
        // scan and applied here instead.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CrossLedgerWebDbContext).Assembly,
            type => type != typeof(TwoFactorCredentialConfiguration) && type != typeof(CardConfiguration));
        modelBuilder.ApplyConfiguration(new TwoFactorCredentialConfiguration(_totpSecretProtector));
        modelBuilder.ApplyConfiguration(new CardConfiguration(_cardDataProtector));
    }
}
