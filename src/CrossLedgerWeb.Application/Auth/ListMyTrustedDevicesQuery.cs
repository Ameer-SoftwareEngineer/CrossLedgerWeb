using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record ListMyTrustedDevicesQuery(UserId UserId) : IRequest<IReadOnlyList<TrustedDeviceSummary>>;

public sealed record TrustedDeviceSummary(
    TrustedDeviceId Id,
    string DeviceLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset ExpiresAt);
