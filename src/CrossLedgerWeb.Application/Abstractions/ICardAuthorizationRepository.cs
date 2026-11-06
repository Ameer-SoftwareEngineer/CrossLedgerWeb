using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface ICardAuthorizationRepository
{
    Task<IReadOnlyList<CardAuthorization>> ListByCardAsync(CardId cardId, CancellationToken cancellationToken);

    /// <summary>Backs the real-time authorization webhook's idempotency check
    /// (specification 5.4's "deduplicated on provider event id", applied here to Stripe's
    /// own authorization id) - if Stripe redelivers the same issuing_authorization.request,
    /// the original decision is returned rather than deciding - and potentially starting a
    /// second hold - twice.</summary>
    Task<CardAuthorization?> GetByStripeAuthorizationIdAsync(string stripeAuthorizationId, CancellationToken cancellationToken);

    /// <summary>Backs the capture webhook's own idempotency check - a redelivered
    /// issuing_transaction.created must not post the same purchase twice.</summary>
    Task<CardAuthorization?> GetByStripeTransactionIdAsync(string stripeTransactionId, CancellationToken cancellationToken);

    /// <summary>The "available to spend" check a new authorization decision makes
    /// (specification 12.3/12.4) - every still-Open hold on this card counts against
    /// available balance even though none of them have touched the ledger yet. Known,
    /// documented race: this is an ordinary read, not locked the way usp_PostTransfer locks
    /// a wallet row - see DecideCardAuthorizationCommandHandler's own doc comment.</summary>
    Task<Money> GetOpenHoldTotalAsync(CardId cardId, Currency currency, CancellationToken cancellationToken);

    /// <summary>Backs the expiry sweeper (specification 12.3/12.4) - every hold still Open
    /// whose ExpiresAt has passed as of the given instant.</summary>
    Task<IReadOnlyList<CardAuthorization>> ListOpenAndExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken);

    /// <summary>Stages a new authorization record for insertion - committed by
    /// UnitOfWorkBehavior's SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(CardAuthorization authorization);
}
