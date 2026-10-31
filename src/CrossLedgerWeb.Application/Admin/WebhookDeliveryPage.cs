namespace CrossLedgerWeb.Application.Admin;

public sealed record WebhookDeliverySummary(
    string EventType, string SubscriberUrl, bool Succeeded, int? ResponseStatusCode, int AttemptCount, DateTimeOffset AttemptedAt);

public sealed record WebhookDeliveryPage(IReadOnlyList<WebhookDeliverySummary> Entries, int TotalCount, int PageNumber, int PageSize);
