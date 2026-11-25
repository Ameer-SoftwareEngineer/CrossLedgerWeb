using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ApproveRegistrationCommandHandler : IRequestHandler<ApproveRegistrationCommand>
{
    private readonly IUserAdminService _userAdmin;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public ApproveRegistrationCommandHandler(
        IUserAdminService userAdmin, ISecurityAuditLogRepository auditLog, IRequestContextAccessor requestContext, IClock clock)
    {
        _userAdmin = userAdmin;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task Handle(ApproveRegistrationCommand request, CancellationToken cancellationToken)
    {
        var found = await _userAdmin.ApproveRegistrationAsync(request.UserId, cancellationToken);
        if (!found)
            throw new UserNotFoundException(request.UserId);

        await _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.AdminUserId, $"Approved registration for {request.UserId}",
                SecurityEventType.RegistrationApproved, succeeded: true, failureReason: null,
                _requestContext.IpAddress, _requestContext.UserAgent, _clock.UtcNow),
            cancellationToken);
    }
}
