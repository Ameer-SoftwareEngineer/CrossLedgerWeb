using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class UnfreezeCardCommandHandler : IRequestHandler<UnfreezeCardCommand>
{
    private readonly ICardRepository _cards;

    public UnfreezeCardCommandHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task Handle(UnfreezeCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        card.Unfreeze();
    }
}
