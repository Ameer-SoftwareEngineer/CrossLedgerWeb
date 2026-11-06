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

    public void Add(CardAuthorization authorization) => _db.CardAuthorizations.Add(authorization);
}
