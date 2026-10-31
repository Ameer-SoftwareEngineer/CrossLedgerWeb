using CrossLedgerWeb.Application.Admin;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Specification 2.6's outbound side: "HMAC-signed payloads with retry and
/// exponential backoff, mirroring how Stripe and similar providers notify merchants."
/// CrossLedger has no multi-tenant merchant/subscriber concept, so there is one configured
/// subscriber endpoint (Webhooks:SubscriberUrl), not a per-merchant registry - the same
/// simplification Limits:RollingTransferLimitPerDay makes for AML limits.</summary>
public interface IWebhookNotifier
{
    /// <summary>Attempts delivery with retry/backoff and records the outcome regardless of
    /// whether it succeeded - never throws, so a subscriber being unreachable can never
    /// fail the caller's own request (same reasoning as the rest of this codebase's
    /// background work: one failure here is logged, not propagated). A no-op, with nothing
    /// recorded, when no subscriber is configured.</summary>
    Task NotifyAsync(string eventType, object payload, CancellationToken cancellationToken);

    /// <summary>Backs the Admin Console's webhook deliveries log (specification 9) - the
    /// outbound counterpart to IProcessedWebhookEventStore.ListRecentAsync's inbound
    /// log.</summary>
    Task<WebhookDeliveryPage> ListRecentAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
