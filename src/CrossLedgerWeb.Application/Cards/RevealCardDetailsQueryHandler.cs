using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Only reachable once the caller has already cleared [RequireStepUp(Operation =
/// StepUpOperation.RevealCardDetails)] at the API layer - this handler itself has no
/// notion of step-up, same separation of concerns as every other step-up-gated action in
/// the codebase.</summary>
public sealed class RevealCardDetailsQueryHandler : IRequestHandler<RevealCardDetailsQuery, RevealCardDetailsResult>
{
    private readonly ICardRepository _cards;

    public RevealCardDetailsQueryHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task<RevealCardDetailsResult> Handle(RevealCardDetailsQuery request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        return new RevealCardDetailsResult(card.Pan, card.Cvv, card.ExpiryMonth, card.ExpiryYear);
    }
}
