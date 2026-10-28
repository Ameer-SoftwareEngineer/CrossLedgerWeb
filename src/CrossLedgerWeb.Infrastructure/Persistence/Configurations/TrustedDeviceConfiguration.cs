using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

public sealed class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.ToTable("TrustedDevices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasConversion(new TrustedDeviceIdConverter()).ValueGeneratedNever();
        builder.Property(d => d.UserId).HasConversion(new UserIdConverter());
        builder.Property(d => d.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(d => d.DeviceLabel).HasMaxLength(128).IsRequired();
        builder.Property(d => d.CreatedAt);
        builder.Property(d => d.ExpiresAt);
        builder.Property(d => d.LastUsedAt);
        builder.Property(d => d.RevokedAt);

        // Unique, matching RefreshTokenConfiguration's TokenHash index - the hash IS
        // the lookup key when a client presents a trusted-device token.
        builder.HasIndex(d => d.TokenHash).IsUnique();
        builder.HasIndex(d => d.UserId);
    }
}
