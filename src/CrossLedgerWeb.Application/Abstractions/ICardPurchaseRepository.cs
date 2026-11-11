using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface ICardPurchaseRepository
{
    Task<IReadOnlyList<CardPurchase>> ListByCardAsync(CardId cardId, CancellationToken cancellationToken);

    /// <summary>Sum of captured purchase amounts on this card posted at/after
    /// <paramref name="since"/> - used for the rolling daily/monthly velocity checks in
    /// DecideCardAuthorizationCommandHandler. Captured spend only (CardPurchase rows),
    /// never open holds - those aren't real spend yet, same distinction "available
    /// balance" already draws.</summary>
    Task<decimal> GetSpendTotalAsync(CardId cardId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Stages a new purchase record for insertion - committed by
    /// UnitOfWorkBehavior's SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(CardPurchase purchase);
}
