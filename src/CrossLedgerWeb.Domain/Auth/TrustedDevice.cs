using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Auth;

/// <summary>An optional, revocable thirty-day "remember this device" grant
/// (specification 6.1) that lets a later login skip the 2FA challenge entirely once
/// the password alone checks out. Mirrors RefreshToken's shape closely - TokenHash,
/// never the raw token, is what's persisted, since this is just as much a long-lived
/// bearer credential as a refresh token is. Deliberately never exempts step-up
/// (specification 6.4): that always re-challenges regardless of how trusted the device
/// is, because step-up is about the freshness of THIS action, not the login session.</summary>
public sealed class TrustedDevice
{
    public TrustedDeviceId Id { get; }
    public UserId UserId { get; }
    public string TokenHash { get; }
    public string DeviceLabel { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public TrustedDevice(
        TrustedDeviceId id,
        UserId userId,
        string tokenHash,
        string deviceLabel,
        DateTimeOffset createdAt,
        TimeSpan validFor)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("A trusted device must have a token hash.", nameof(tokenHash));

        if (string.IsNullOrWhiteSpace(deviceLabel))
            throw new ArgumentException("A trusted device must have a label.", nameof(deviceLabel));

        if (validFor <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(validFor), validFor, "A trusted device grant must be valid for a positive duration.");

        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        DeviceLabel = deviceLabel;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + validFor;
    }

    /// <summary>Rehydration constructor for EF Core materialization - same ExpiresAt
    /// binding issue RefreshToken's private constructor exists for. LastUsedAt and
    /// RevokedAt are left out too; EF sets them directly via their private setters.</summary>
    private TrustedDevice(
        TrustedDeviceId id,
        UserId userId,
        string tokenHash,
        string deviceLabel,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        DeviceLabel = deviceLabel;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset asOf) => asOf >= ExpiresAt;

    public bool IsActive(DateTimeOffset asOf) => !IsRevoked && !IsExpired(asOf);

    public void MarkUsed(DateTimeOffset usedAt) => LastUsedAt = usedAt;

    /// <summary>Idempotent, matching RefreshToken.Revoke - only the first revocation's
    /// timestamp is kept.</summary>
    public void Revoke(DateTimeOffset revokedAt)
    {
        if (IsRevoked)
            return;

        RevokedAt = revokedAt;
    }
}
