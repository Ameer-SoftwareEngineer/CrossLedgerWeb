using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class CardRepository : ICardRepository
{
    private readonly CrossLedgerWebDbContext _db;

    public CardRepository(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public Task<Card?> GetByIdAsync(CardId id, CancellationToken cancellationToken) =>
        _db.Cards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Card>> ListByOwnerAsync(UserId ownerId, CancellationToken cancellationToken) =>
        await _db.Cards.Where(c => c.OwnerId == ownerId).ToListAsync(cancellationToken);

    public void Add(Card card) => _db.Cards.Add(card);
}
