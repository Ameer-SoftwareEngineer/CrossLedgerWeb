using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListWebhookEventsQuery(int PageNumber, int PageSize) : IRequest<WebhookEventPage>;
