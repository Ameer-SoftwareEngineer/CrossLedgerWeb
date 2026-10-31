using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasConversion(new PayoutIdConverter()).ValueGeneratedNever();
        builder.Property(p => p.TransferId).HasConversion(new TransferIdConverter());
        builder.Property(p => p.SourceWalletId).HasConversion(new WalletIdConverter());
        builder.Property(p => p.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.ProviderCode).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.ProviderReference).HasMaxLength(256);
        builder.Property(p => p.UpdatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        // Money is a struct, so it maps as an EF Core 8 complex type (ComplexProperty),
        // not OwnsOne - same reasoning as LedgerEntryConfiguration.
        builder.ComplexProperty(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(19,4)");
            money.Property(m => m.Currency).HasConversion(new CurrencyConverter()).HasColumnName("CurrencyCode").HasMaxLength(3);
        });

        builder.HasIndex(p => p.TransferId);

        // What ReceivePayoutWebhookCommandHandler looks a payout up by - filtered because
        // ProviderReference is only set once a provider has accepted (ProviderCode.cs's
        // own doc comment on Payout explains why it starts null).
        builder.HasIndex(p => new { p.ProviderCode, p.ProviderReference })
            .IsUnique()
            .HasFilter("[ProviderReference] IS NOT NULL");

        // usp_GetProviderStats' rolling-24h window filters by exactly these two columns
        // (specification 5.3) - same indexing rationale as usp_CheckTransferLimits' own
        // doc comment on Wallets(OwnerId).
        builder.HasIndex(p => new { p.ProviderCode, p.UpdatedAt });
    }
}
