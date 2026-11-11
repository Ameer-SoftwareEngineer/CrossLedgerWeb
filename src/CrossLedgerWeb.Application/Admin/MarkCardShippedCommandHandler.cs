using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class MarkCardShippedCommandHandler : IRequestHandler<MarkCardShippedCommand>
{
    private readonly ICardRepository _cards;
    private readonly IClock _clock;

    public MarkCardShippedCommandHandler(ICardRepository cards, IClock clock)
    {
        _cards = cards;
        _clock = clock;
    }

    public async Task Handle(MarkCardShippedCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new CardNotFoundException(request.CardId);

        card.MarkShipped(request.Carrier, request.TrackingNumber, _clock.UtcNow);
    }
}
