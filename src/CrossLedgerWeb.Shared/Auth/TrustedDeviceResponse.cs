namespace CrossLedgerWeb.Shared.Auth;

public sealed record TrustedDeviceResponse(
    Guid Id,
    string DeviceLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset ExpiresAt);
