using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListWebhookEventsQueryHandler : IRequestHandler<ListWebhookEventsQuery, WebhookEventPage>
{
    private readonly IProcessedWebhookEventStore _events;

    public ListWebhookEventsQueryHandler(IProcessedWebhookEventStore events)
    {
        _events = events;
    }

    public Task<WebhookEventPage> Handle(ListWebhookEventsQuery request, CancellationToken cancellationToken) =>
        _events.ListRecentAsync(
            request.PageNumber <= 0 ? 1 : request.PageNumber,
            request.PageSize <= 0 ? 20 : request.PageSize,
            cancellationToken);
}
