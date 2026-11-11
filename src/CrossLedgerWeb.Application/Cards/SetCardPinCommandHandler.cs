using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class SetCardPinCommandHandler : IRequestHandler<SetCardPinCommand>
{
    private readonly ICardRepository _cards;

    public SetCardPinCommandHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task Handle(SetCardPinCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        card.SetPin(request.Pin);
    }
}
