using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.Payments;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Deduplicates inbound provider webhooks on (provider, event id) before any
/// ledger entry is written - "providers do not guarantee once-only delivery"
/// (specification 5.4). Deliberately its own store rather than reusing IIdempotencyStore:
/// that one is wired specifically to MediatR's request/response pipeline, replaying a
/// serialized command *response* for a repeated key - a webhook has no MediatR request
/// wrapping it and nothing typed to replay, it only needs a seen/unseen answer.</summary>
public interface IProcessedWebhookEventStore
{
    Task<bool> HasBeenProcessedAsync(ProviderCode providerCode, string eventId, CancellationToken cancellationToken);

    /// <summary>Stages the event as processed - committed by UnitOfWorkBehavior's
    /// SaveChanges at the end of the pipeline, not immediately.</summary>
    void MarkProcessed(ProviderCode providerCode, string eventId, DateTimeOffset processedAt);

    /// <summary>Backs the Admin Console's webhook log (specification 9) - this is the
    /// dedup record of inbound provider notifications CrossLedger has already handled,
    /// not an outbound delivery log (this codebase has no outbound webhook-subscriber
    /// concept to log).</summary>
    Task<WebhookEventPage> ListRecentAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
