using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class CardPurchaseRepository : ICardPurchaseRepository
{
    private readonly CrossLedgerWebDbContext _db;

    public CardPurchaseRepository(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CardPurchase>> ListByCardAsync(CardId cardId, CancellationToken cancellationToken) =>
        await _db.CardPurchases.Where(p => p.CardId == cardId).ToListAsync(cancellationToken);

    public void Add(CardPurchase purchase) => _db.CardPurchases.Add(purchase);
}
