using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface ICardAuthorizationRepository
{
    Task<IReadOnlyList<CardAuthorization>> ListByCardAsync(CardId cardId, CancellationToken cancellationToken);

    /// <summary>Backs the real-time authorization webhook's idempotency check
    /// (specification 5.4's "deduplicated on provider event id", applied here to Stripe's
    /// own authorization id) - if Stripe redelivers the same issuing_authorization.request,
    /// the original decision is returned rather than deciding - and potentially posting a
    /// second purchase - twice.</summary>
    Task<CardAuthorization?> GetByStripeAuthorizationIdAsync(string stripeAuthorizationId, CancellationToken cancellationToken);

    /// <summary>Stages a new authorization record for insertion - committed by
    /// UnitOfWorkBehavior's SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(CardAuthorization authorization);
}
