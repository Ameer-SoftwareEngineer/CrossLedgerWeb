using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class RejectCardRequestCommandHandler : IRequestHandler<RejectCardRequestCommand>
{
    private readonly ICardRepository _cards;

    public RejectCardRequestCommandHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task Handle(RejectCardRequestCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new CardNotFoundException(request.CardId);

        card.Reject();
    }
}
