using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class MarkCardShippedCommandHandler : IRequestHandler<MarkCardShippedCommand>
{
    private readonly ICardRepository _cards;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public MarkCardShippedCommandHandler(
        ICardRepository cards, ISecurityAuditLogRepository auditLog, IRequestContextAccessor requestContext, IClock clock)
    {
        _cards = cards;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task Handle(MarkCardShippedCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new CardNotFoundException(request.CardId);

        var now = _clock.UtcNow;
        card.MarkShipped(request.Carrier, request.TrackingNumber, now);

        await _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.AdminUserId, $"Marked card {request.CardId} shipped via {request.Carrier} ({request.TrackingNumber})",
                SecurityEventType.CardMarkedShipped, succeeded: true, failureReason: null,
                _requestContext.IpAddress, _requestContext.UserAgent, now),
            cancellationToken);
    }
}
