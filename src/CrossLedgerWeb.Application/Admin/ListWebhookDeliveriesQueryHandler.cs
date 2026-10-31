using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListWebhookDeliveriesQueryHandler : IRequestHandler<ListWebhookDeliveriesQuery, WebhookDeliveryPage>
{
    private readonly IWebhookNotifier _notifier;

    public ListWebhookDeliveriesQueryHandler(IWebhookNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task<WebhookDeliveryPage> Handle(ListWebhookDeliveriesQuery request, CancellationToken cancellationToken) =>
        _notifier.ListRecentAsync(
            request.PageNumber <= 0 ? 1 : request.PageNumber,
            request.PageSize <= 0 ? 20 : request.PageSize,
            cancellationToken);
}
