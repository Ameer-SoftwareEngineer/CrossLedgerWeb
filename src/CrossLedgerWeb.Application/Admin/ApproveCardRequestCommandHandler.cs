using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ApproveCardRequestCommandHandler : IRequestHandler<ApproveCardRequestCommand>
{
    private readonly ICardRepository _cards;

    public ApproveCardRequestCommandHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task Handle(ApproveCardRequestCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new CardNotFoundException(request.CardId);

        card.Approve();
    }
}
