using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class RevokeTrustedDeviceCommandHandler : IRequestHandler<RevokeTrustedDeviceCommand>
{
    private readonly ITrustedDeviceRepository _devices;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public RevokeTrustedDeviceCommandHandler(
        ITrustedDeviceRepository devices,
        ISecurityAuditLogRepository auditLog,
        IRequestContextAccessor requestContext,
        IClock clock)
    {
        _devices = devices;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task Handle(RevokeTrustedDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _devices.GetByIdAsync(request.TrustedDeviceId, cancellationToken);
        if (device is null || device.UserId != request.UserId)
            throw new TrustedDeviceNotFoundException(request.TrustedDeviceId);

        var now = _clock.UtcNow;
        device.Revoke(now);

        await _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.UserId, device.DeviceLabel, SecurityEventType.TrustedDeviceRevoked, succeeded: true,
                failureReason: null, _requestContext.IpAddress, _requestContext.UserAgent, now),
            cancellationToken);
    }
}
