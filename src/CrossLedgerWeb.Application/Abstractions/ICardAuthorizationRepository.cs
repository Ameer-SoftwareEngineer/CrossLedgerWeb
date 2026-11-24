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

    /// <summary>An ordinary, unlocked read of every still-Open hold's total on this card -
    /// informational only (e.g. display). The actual "is there enough available balance"
    /// decision a new authorization makes no longer goes through this method; it is made
    /// atomically, under lock, by ICardAuthorizationHoldReservation
    /// (dbo.usp_ReserveCardAuthorizationHold) instead - see
    /// DecideCardAuthorizationCommandHandler's own doc comment.</summary>
    Task<Money> GetOpenHoldTotalAsync(CardId cardId, Currency currency, CancellationToken cancellationToken);

    /// <summary>Backs the expiry sweeper (specification 12.3/12.4) - every hold still Open
    /// whose ExpiresAt has passed as of the given instant.</summary>
    Task<IReadOnlyList<CardAuthorization>> ListOpenAndExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken);

    /// <summary>Backs the admin "pending manual review" tab - every hold an over-capture
    /// flagged via CardAuthorization.FlagForManualReview.</summary>
    Task<IReadOnlyList<CardAuthorization>> ListPendingManualReviewAsync(CancellationToken cancellationToken);

    /// <summary>Loads by our own id, not Stripe's - backs the admin manual-review resolve
    /// action, which the admin addresses by CardAuthorizationId (from the pending-review
    /// list), not by any Stripe identifier.</summary>
    Task<CardAuthorization?> GetByIdAsync(CardAuthorizationId id, CancellationToken cancellationToken);

    /// <summary>Stages a new authorization record for insertion - committed by
    /// UnitOfWorkBehavior's SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(CardAuthorization authorization);
}
