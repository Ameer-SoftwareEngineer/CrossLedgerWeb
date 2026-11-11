using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class ActivateCardCommandHandler : IRequestHandler<ActivateCardCommand>
{
    private readonly ICardRepository _cards;
    private readonly IClock _clock;

    public ActivateCardCommandHandler(ICardRepository cards, IClock clock)
    {
        _cards = cards;
        _clock = clock;
    }

    public async Task Handle(ActivateCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        card.Activate(_clock.UtcNow);
    }
}
