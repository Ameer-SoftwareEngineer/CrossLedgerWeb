namespace CrossLedgerWeb.Application.Admin;

public sealed record WebhookEventSummary(string ProviderCode, string EventId, DateTimeOffset ProcessedAt);

public sealed record WebhookEventPage(IReadOnlyList<WebhookEventSummary> Entries, int TotalCount, int PageNumber, int PageSize);
