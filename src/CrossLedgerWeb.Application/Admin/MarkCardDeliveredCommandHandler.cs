using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class MarkCardDeliveredCommandHandler : IRequestHandler<MarkCardDeliveredCommand>
{
    private readonly ICardRepository _cards;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public MarkCardDeliveredCommandHandler(
        ICardRepository cards, ISecurityAuditLogRepository auditLog, IRequestContextAccessor requestContext, IClock clock)
    {
        _cards = cards;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task Handle(MarkCardDeliveredCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new CardNotFoundException(request.CardId);

        var now = _clock.UtcNow;
        card.MarkDelivered(now);

        await _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.AdminUserId, $"Marked card {request.CardId} delivered",
                SecurityEventType.CardMarkedDelivered, succeeded: true, failureReason: null,
                _requestContext.IpAddress, _requestContext.UserAgent, now),
            cancellationToken);
    }
}
