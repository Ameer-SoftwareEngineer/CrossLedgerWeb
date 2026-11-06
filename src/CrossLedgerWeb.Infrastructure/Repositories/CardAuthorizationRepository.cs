using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class CardAuthorizationRepository : ICardAuthorizationRepository
{
    private readonly CrossLedgerWebDbContext _db;

    public CardAuthorizationRepository(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CardAuthorization>> ListByCardAsync(CardId cardId, CancellationToken cancellationToken) =>
        await _db.CardAuthorizations.Where(a => a.CardId == cardId).ToListAsync(cancellationToken);

    public Task<CardAuthorization?> GetByStripeAuthorizationIdAsync(string stripeAuthorizationId, CancellationToken cancellationToken) =>
        _db.CardAuthorizations.FirstOrDefaultAsync(a => a.StripeAuthorizationId == stripeAuthorizationId, cancellationToken);

    public Task<CardAuthorization?> GetByStripeTransactionIdAsync(string stripeTransactionId, CancellationToken cancellationToken) =>
        _db.CardAuthorizations.FirstOrDefaultAsync(a => a.StripeTransactionId == stripeTransactionId, cancellationToken);

    public async Task<Money> GetOpenHoldTotalAsync(CardId cardId, Currency currency, CancellationToken cancellationToken)
    {
        var hasOpenHolds = await _db.CardAuthorizations
            .AnyAsync(a => a.CardId == cardId && a.HoldStatus == CardAuthorizationHoldStatus.Open, cancellationToken);
        if (!hasOpenHolds)
            return Money.Zero(currency);

        var total = await _db.CardAuthorizations
            .Where(a => a.CardId == cardId && a.HoldStatus == CardAuthorizationHoldStatus.Open)
            .SumAsync(a => a.Amount.Amount, cancellationToken);
        return new Money(total, currency);
    }

    public async Task<IReadOnlyList<CardAuthorization>> ListOpenAndExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken) =>
        await _db.CardAuthorizations
            .Where(a => a.HoldStatus == CardAuthorizationHoldStatus.Open && a.ExpiresAt <= asOf)
            .ToListAsync(cancellationToken);

    public void Add(CardAuthorization authorization) => _db.CardAuthorizations.Add(authorization);
}
