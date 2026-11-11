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

    public async Task<decimal> GetSpendTotalAsync(CardId cardId, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var hasAny = await _db.CardPurchases.AnyAsync(p => p.CardId == cardId && p.PostedAt >= since, cancellationToken);
        if (!hasAny)
            return 0m;

        return await _db.CardPurchases
            .Where(p => p.CardId == cardId && p.PostedAt >= since)
            .SumAsync(p => p.Amount.Amount, cancellationToken);
    }

    public void Add(CardPurchase purchase) => _db.CardPurchases.Add(purchase);
}
