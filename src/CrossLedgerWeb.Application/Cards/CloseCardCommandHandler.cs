using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class CloseCardCommandHandler : IRequestHandler<CloseCardCommand>
{
    private readonly ICardRepository _cards;
    private readonly IClock _clock;

    public CloseCardCommandHandler(ICardRepository cards, IClock clock)
    {
        _cards = cards;
        _clock = clock;
    }

    public async Task Handle(CloseCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        card.Close(_clock.UtcNow);
    }
}
