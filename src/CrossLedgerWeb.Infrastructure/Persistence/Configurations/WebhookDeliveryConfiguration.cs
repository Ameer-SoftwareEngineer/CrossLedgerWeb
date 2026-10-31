using CrossLedgerWeb.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.EventType).HasMaxLength(100);
        builder.Property(d => d.Payload).HasMaxLength(4000);
        builder.Property(d => d.SubscriberUrl).HasMaxLength(2048);

        builder.HasIndex(d => d.AttemptedAt);
    }
}
