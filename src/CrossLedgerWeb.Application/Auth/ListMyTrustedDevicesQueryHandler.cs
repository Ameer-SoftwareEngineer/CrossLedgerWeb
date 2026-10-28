using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class ListMyTrustedDevicesQueryHandler : IRequestHandler<ListMyTrustedDevicesQuery, IReadOnlyList<TrustedDeviceSummary>>
{
    private readonly ITrustedDeviceRepository _devices;
    private readonly IClock _clock;

    public ListMyTrustedDevicesQueryHandler(ITrustedDeviceRepository devices, IClock clock)
    {
        _devices = devices;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TrustedDeviceSummary>> Handle(ListMyTrustedDevicesQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var devices = await _devices.GetByUserIdAsync(request.UserId, cancellationToken);

        return devices
            .Where(d => d.IsActive(now))
            .OrderByDescending(d => d.LastUsedAt ?? d.CreatedAt)
            .Select(d => new TrustedDeviceSummary(d.Id, d.DeviceLabel, d.CreatedAt, d.LastUsedAt, d.ExpiresAt))
            .ToList();
    }
}
