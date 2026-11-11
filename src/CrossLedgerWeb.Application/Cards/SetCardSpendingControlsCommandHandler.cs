using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed class SetCardSpendingControlsCommandHandler : IRequestHandler<SetCardSpendingControlsCommand>
{
    private readonly ICardRepository _cards;

    public SetCardSpendingControlsCommandHandler(ICardRepository cards)
    {
        _cards = cards;
    }

    public async Task Handle(SetCardSpendingControlsCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        card.SetSpendingControls(request.BlockedMerchantCategoryCodes, request.AllowedCountries, request.DailyLimit, request.MonthlyLimit);
    }
}
