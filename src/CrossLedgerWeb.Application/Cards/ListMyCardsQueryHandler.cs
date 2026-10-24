using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class ListMyCardsQueryHandler : IRequestHandler<ListMyCardsQuery, IReadOnlyList<CardSummary>>
{
    private readonly ICardRepository _cards;
    private readonly IWalletRepository _wallets;

    public ListMyCardsQueryHandler(ICardRepository cards, IWalletRepository wallets)
    {
        _cards = cards;
        _wallets = wallets;
    }

    public async Task<IReadOnlyList<CardSummary>> Handle(ListMyCardsQuery request, CancellationToken cancellationToken)
    {
        var cards = await _cards.ListByOwnerAsync(request.OwnerId, cancellationToken);
        var summaries = new List<CardSummary>(cards.Count);

        foreach (var card in cards.OrderByDescending(c => c.IssuedAt))
        {
            var wallet = await _wallets.GetByIdAsync(card.CardWalletId, cancellationToken);
            var balance = wallet?.Balance.Amount ?? 0m;

            summaries.Add(new CardSummary(
                card.Id, card.CardWalletId, card.Currency, card.CardType, card.CardholderName, card.Last4,
                card.ExpiryMonth, card.ExpiryYear, card.Status, card.SpendingLimit, balance, card.IssuedAt));
        }

        return summaries;
    }
}
