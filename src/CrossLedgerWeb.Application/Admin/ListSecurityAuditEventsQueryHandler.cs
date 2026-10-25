using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListSecurityAuditEventsQueryHandler : IRequestHandler<ListSecurityAuditEventsQuery, SecurityAuditEventPage>
{
    private readonly ISecurityAuditLogRepository _events;

    public ListSecurityAuditEventsQueryHandler(ISecurityAuditLogRepository events)
    {
        _events = events;
    }

    public Task<SecurityAuditEventPage> Handle(ListSecurityAuditEventsQuery request, CancellationToken cancellationToken) =>
        _events.ListRecentAsync(
            request.PageNumber <= 0 ? 1 : request.PageNumber,
            request.PageSize <= 0 ? 20 : request.PageSize,
            cancellationToken);
}
