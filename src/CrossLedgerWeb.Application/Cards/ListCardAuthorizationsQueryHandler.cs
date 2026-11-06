using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class ListCardAuthorizationsQueryHandler : IRequestHandler<ListCardAuthorizationsQuery, IReadOnlyList<CardAuthorizationSummary>>
{
    private readonly ICardRepository _cards;
    private readonly ICardAuthorizationRepository _authorizations;

    public ListCardAuthorizationsQueryHandler(ICardRepository cards, ICardAuthorizationRepository authorizations)
    {
        _cards = cards;
        _authorizations = authorizations;
    }

    public async Task<IReadOnlyList<CardAuthorizationSummary>> Handle(ListCardAuthorizationsQuery request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        var authorizations = await _authorizations.ListByCardAsync(request.CardId, cancellationToken);

        return authorizations
            .OrderByDescending(a => a.DecidedAt)
            .Select(a => new CardAuthorizationSummary(a.Id, a.MerchantName, a.Amount, a.Approved, a.DeclineReason, a.DecidedAt))
            .ToList();
    }
}
