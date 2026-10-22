using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class SetCardSpendingLimitCommandHandler : IRequestHandler<SetCardSpendingLimitCommand>
{
    private readonly ICardRepository _cards;

    public SetCardSpendingLimitCommandHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task Handle(SetCardSpendingLimitCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        card.SetSpendingLimit(request.Limit);
    }
}
