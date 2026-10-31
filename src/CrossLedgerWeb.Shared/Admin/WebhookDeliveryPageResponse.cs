namespace CrossLedgerWeb.Shared.Admin;

public sealed record WebhookDeliveryResponse(
    string EventType, string SubscriberUrl, bool Succeeded, int? ResponseStatusCode, int AttemptCount, DateTimeOffset AttemptedAt);

public sealed record WebhookDeliveryPageResponse(IReadOnlyList<WebhookDeliveryResponse> Entries, int TotalCount, int PageNumber, int PageSize);
