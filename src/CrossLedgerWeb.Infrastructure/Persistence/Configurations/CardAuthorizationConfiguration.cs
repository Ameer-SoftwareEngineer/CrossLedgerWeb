using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

public sealed class CardAuthorizationConfiguration : IEntityTypeConfiguration<CardAuthorization>
{
    public void Configure(EntityTypeBuilder<CardAuthorization> builder)
    {
        builder.ToTable("CardAuthorizations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasConversion(new CardAuthorizationIdConverter()).ValueGeneratedNever();
        builder.Property(a => a.CardId).HasConversion(new CardIdConverter());
        builder.Property(a => a.StripeAuthorizationId).HasMaxLength(255);
        builder.Property(a => a.MerchantName).HasMaxLength(100);
        builder.Property(a => a.Approved);
        builder.Property(a => a.DeclineReason).HasMaxLength(50);
        builder.Property(a => a.TransferId).HasConversion(new TransferIdConverter());
        builder.Property(a => a.DecidedAt);

        builder.ComplexProperty(a => a.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(19,4)");
            money.Property(m => m.Currency).HasConversion(new CurrencyConverter()).HasColumnName("CurrencyCode").HasMaxLength(3);
        });

        builder.HasIndex(a => a.CardId);
        builder.HasIndex(a => a.StripeAuthorizationId).IsUnique();
    }
}
