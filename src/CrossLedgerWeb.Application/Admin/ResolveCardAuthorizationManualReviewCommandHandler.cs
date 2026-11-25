using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ResolveCardAuthorizationManualReviewCommandHandler : IRequestHandler<ResolveCardAuthorizationManualReviewCommand>
{
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public ResolveCardAuthorizationManualReviewCommandHandler(
        ICardAuthorizationRepository authorizations, ISecurityAuditLogRepository auditLog, IRequestContextAccessor requestContext, IClock clock)
    {
        _authorizations = authorizations;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task Handle(ResolveCardAuthorizationManualReviewCommand request, CancellationToken cancellationToken)
    {
        var authorization = await _authorizations.GetByIdAsync(request.CardAuthorizationId, cancellationToken)
            ?? throw new CardAuthorizationNotFoundException(request.CardAuthorizationId);

        var now = _clock.UtcNow;
        authorization.ResolveManualReview(now);

        await _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.AdminUserId, $"Resolved manual review for card authorization {request.CardAuthorizationId}",
                SecurityEventType.ManualReviewResolved, succeeded: true, failureReason: null,
                _requestContext.IpAddress, _requestContext.UserAgent, now),
            cancellationToken);
    }
}
