using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand>
{
    private readonly ITwoFactorCredentialRepository _credentials;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public DisableTwoFactorCommandHandler(
        ITwoFactorCredentialRepository credentials,
        ISecurityAuditLogRepository auditLog,
        IRequestContextAccessor requestContext,
        IClock clock)
    {
        _credentials = credentials;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var credential = await _credentials.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new TwoFactorNotEnabledException();

        credential.Disable();

        await _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.UserId, request.UserId.ToString(), SecurityEventType.TwoFactorDisabled,
                succeeded: true, failureReason: null, _requestContext.IpAddress, _requestContext.UserAgent, _clock.UtcNow),
            cancellationToken);
    }
}
