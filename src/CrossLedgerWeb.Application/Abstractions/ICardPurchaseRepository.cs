using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface ICardPurchaseRepository
{
    Task<IReadOnlyList<CardPurchase>> ListByCardAsync(CardId cardId, CancellationToken cancellationToken);

    /// <summary>Stages a new purchase record for insertion - committed by
    /// UnitOfWorkBehavior's SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(CardPurchase purchase);
}
