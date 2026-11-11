using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class MarkCardDeliveredCommandHandler : IRequestHandler<MarkCardDeliveredCommand>
{
    private readonly ICardRepository _cards;
    private readonly IClock _clock;

    public MarkCardDeliveredCommandHandler(ICardRepository cards, IClock clock)
    {
        _cards = cards;
        _clock = clock;
    }

    public async Task Handle(MarkCardDeliveredCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new CardNotFoundException(request.CardId);

        card.MarkDelivered(_clock.UtcNow);
    }
}
