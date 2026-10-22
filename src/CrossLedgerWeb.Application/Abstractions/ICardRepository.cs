using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface ICardRepository
{
    Task<Card?> GetByIdAsync(CardId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Card>> ListByOwnerAsync(UserId ownerId, CancellationToken cancellationToken);

    /// <summary>Stages a new card for insertion - committed by UnitOfWorkBehavior's
    /// SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(Card card);
}
