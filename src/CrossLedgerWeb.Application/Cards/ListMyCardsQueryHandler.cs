using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class ListMyCardsQueryHandler : IRequestHandler<ListMyCardsQuery, IReadOnlyList<CardSummary>>
{
    private readonly ICardRepository _cards;

    public ListMyCardsQueryHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task<IReadOnlyList<CardSummary>> Handle(ListMyCardsQuery request, CancellationToken cancellationToken)
    {
        var cards = await _cards.ListByOwnerAsync(request.OwnerId, cancellationToken);

        return cards
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new CardSummary(
                c.Id, c.LinkedWalletId, c.Currency, c.CardholderName, c.Last4,
                c.ExpiryMonth, c.ExpiryYear, c.Status, c.SpendingLimit, c.IssuedAt))
            .ToList();
    }
}
