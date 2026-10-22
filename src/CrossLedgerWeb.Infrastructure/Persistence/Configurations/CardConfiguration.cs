using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLedgerWeb.Infrastructure.Persistence.Configurations;

/// <summary>
/// Not picked up by ApplyConfigurationsFromAssembly - it needs an IDataProtector, which
/// only the DbContext (resolved via DI) can supply, so CrossLedgerWebDbContext excludes
/// this type from the assembly scan and applies it manually with a protector passed in,
/// same pattern as TwoFactorCredentialConfiguration. Pan and Cvv are encrypted at rest;
/// the domain entity itself holds them as plaintext, decrypted transparently by this
/// conversion on read - "reveal" is simply reading the entity, gated by step-up at the
/// API layer, not by anything the entity or this configuration does.
/// </summary>
public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    private readonly IDataProtector _cardDataProtector;

    public CardConfiguration(IDataProtector cardDataProtector)
    {
        _cardDataProtector = cardDataProtector;
    }

    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasConversion(new CardIdConverter()).ValueGeneratedNever();
        builder.Property(c => c.OwnerId).HasConversion(new UserIdConverter());
        builder.Property(c => c.LinkedWalletId).HasConversion(new WalletIdConverter());
        builder.Property(c => c.Currency).HasConversion(new CurrencyConverter()).HasColumnName("CurrencyCode").HasMaxLength(3);
        builder.Property(c => c.CardholderName).HasMaxLength(100);

        builder.Property(c => c.Pan)
            .HasConversion(plain => _cardDataProtector.Protect(plain), encrypted => _cardDataProtector.Unprotect(encrypted))
            .HasColumnName("EncryptedPan")
            .HasMaxLength(512);

        builder.Property(c => c.Cvv)
            .HasConversion(plain => _cardDataProtector.Protect(plain), encrypted => _cardDataProtector.Unprotect(encrypted))
            .HasColumnName("EncryptedCvv")
            .HasMaxLength(512);

        builder.Property(c => c.ExpiryMonth);
        builder.Property(c => c.ExpiryYear);
        builder.Property(c => c.IssuedAt);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.SpendingLimit).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ClosedAt);

        // Last4 is derived from Pan, never its own column.
        builder.Ignore(c => c.Last4);

        builder.HasIndex(c => c.OwnerId);
    }
}
