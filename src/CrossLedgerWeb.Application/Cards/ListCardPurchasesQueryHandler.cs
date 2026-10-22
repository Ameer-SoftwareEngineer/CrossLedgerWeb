using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class ListCardPurchasesQueryHandler : IRequestHandler<ListCardPurchasesQuery, IReadOnlyList<CardPurchaseSummary>>
{
    private readonly ICardRepository _cards;
    private readonly ICardPurchaseRepository _purchases;

    public ListCardPurchasesQueryHandler(ICardRepository cards, ICardPurchaseRepository purchases)
    {
        _cards = cards;
        _purchases = purchases;
    }

    public async Task<IReadOnlyList<CardPurchaseSummary>> Handle(ListCardPurchasesQuery request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        var purchases = await _purchases.ListByCardAsync(request.CardId, cancellationToken);

        return purchases
            .OrderByDescending(p => p.PostedAt)
            .Select(p => new CardPurchaseSummary(p.Id, p.TransferId, p.MerchantName, p.Amount, p.PostedAt))
            .ToList();
    }
}
