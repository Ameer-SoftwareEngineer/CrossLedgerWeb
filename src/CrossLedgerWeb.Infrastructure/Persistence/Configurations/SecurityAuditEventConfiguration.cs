using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

public sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        builder.ToTable("SecurityAuditEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasConversion(new SecurityAuditEventIdConverter()).ValueGeneratedNever();
        builder.Property(e => e.UserId).HasConversion(new NullableUserIdConverter());
        builder.Property(e => e.Subject).HasMaxLength(256).IsRequired();
        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Succeeded);
        builder.Property(e => e.FailureReason).HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
        builder.Property(e => e.OccurredAt);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.OccurredAt);
    }
}
