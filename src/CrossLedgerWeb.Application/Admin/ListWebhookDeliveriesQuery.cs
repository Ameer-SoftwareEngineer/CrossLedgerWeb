using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListWebhookDeliveriesQuery(int PageNumber, int PageSize) : IRequest<WebhookDeliveryPage>;
