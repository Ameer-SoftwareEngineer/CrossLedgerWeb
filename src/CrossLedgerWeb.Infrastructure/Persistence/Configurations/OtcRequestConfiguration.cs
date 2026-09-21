using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

public sealed class OtcRequestConfiguration : IEntityTypeConfiguration<OtcRequest>
{
    public void Configure(EntityTypeBuilder<OtcRequest> builder)
    {
        builder.ToTable("OtcRequests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasConversion(new OtcRequestIdConverter()).ValueGeneratedNever();
        builder.Property(r => r.RequestedByUserId).HasConversion(new UserIdConverter());
        builder.Property(r => r.SourceWalletId).HasConversion(new WalletIdConverter());
        builder.Property(r => r.QuoteId).HasConversion(new QuoteIdConverter());
        builder.Property(r => r.FromCurrency).HasConversion(new CurrencyConverter()).HasColumnName("FromCurrencyCode").HasMaxLength(3);
        builder.Property(r => r.ToCurrency).HasConversion(new CurrencyConverter()).HasColumnName("ToCurrencyCode").HasMaxLength(3);
        builder.Property(r => r.RequestedAmount).HasColumnType("decimal(18,8)");
        builder.Property(r => r.DeskRate).HasColumnType("decimal(18,8)");
        builder.Property(r => r.RequestedAt);
        builder.Property(r => r.OfferExpiresAt);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.TransferId);

        builder.HasIndex(r => r.RequestedByUserId);
        builder.HasIndex(r => r.OfferExpiresAt);
    }
}
