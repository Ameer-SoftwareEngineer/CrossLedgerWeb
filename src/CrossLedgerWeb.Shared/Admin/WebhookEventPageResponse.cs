namespace CrossLedgerWeb.Shared.Admin;

public sealed record WebhookEventResponse(string ProviderCode, string EventId, DateTimeOffset ProcessedAt);

public sealed record WebhookEventPageResponse(IReadOnlyList<WebhookEventResponse> Entries, int TotalCount, int PageNumber, int PageSize);
