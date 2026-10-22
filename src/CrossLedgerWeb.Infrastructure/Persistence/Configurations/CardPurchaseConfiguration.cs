using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

public sealed class CardPurchaseConfiguration : IEntityTypeConfiguration<CardPurchase>
{
    public void Configure(EntityTypeBuilder<CardPurchase> builder)
    {
        builder.ToTable("CardPurchases");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasConversion(new CardPurchaseIdConverter()).ValueGeneratedNever();
        builder.Property(p => p.CardId).HasConversion(new CardIdConverter());
        builder.Property(p => p.TransferId).HasConversion(new TransferIdConverter());
        builder.Property(p => p.MerchantName).HasMaxLength(100);
        builder.Property(p => p.PostedAt);

        builder.ComplexProperty(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(19,4)");
            money.Property(m => m.Currency).HasConversion(new CurrencyConverter()).HasColumnName("CurrencyCode").HasMaxLength(3);
        });

        builder.HasIndex(p => p.CardId);
        builder.HasIndex(p => p.TransferId);
    }
}
