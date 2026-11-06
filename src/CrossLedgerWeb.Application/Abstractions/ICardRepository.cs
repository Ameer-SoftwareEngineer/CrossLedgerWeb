using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface ICardRepository
{
    Task<Card?> GetByIdAsync(CardId id, CancellationToken cancellationToken);

    /// <summary>Backs the real-time authorization webhook (specification 12.3) - Stripe
    /// identifies the card by its own id, never ours, so this is the only lookup path that
    /// webhook has. Returns null for any card issued via SimulatedCardIssuer, which never
    /// sets StripeCardId - the caller treats that the same as "unknown card".</summary>
    Task<Card?> GetByStripeCardIdAsync(string stripeCardId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Card>> ListByOwnerAsync(UserId ownerId, CancellationToken cancellationToken);

    /// <summary>Backs the Admin Console's card issuance review (specification 9) - every
    /// card still awaiting Approve/Reject, oldest first.</summary>
    Task<IReadOnlyList<Card>> ListPendingApprovalAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new card for insertion - committed by UnitOfWorkBehavior's
    /// SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(Card card);
}
