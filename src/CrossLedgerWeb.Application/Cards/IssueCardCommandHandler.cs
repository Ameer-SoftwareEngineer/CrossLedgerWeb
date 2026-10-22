using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Issues a dev-simulated virtual card linked to one of the caller's own
/// wallets - no real card network or issuing processor involved, same spirit as
/// DevLogSmsSender simulating an SMS gateway.</summary>
public sealed class IssueCardCommandHandler : IRequestHandler<IssueCardCommand, IssueCardResult>
{
    private const int ValidityYears = 3;

    private readonly IWalletRepository _wallets;
    private readonly ICardRepository _cards;
    private readonly ICardNumberGenerator _numberGenerator;
    private readonly IClock _clock;

    public IssueCardCommandHandler(IWalletRepository wallets, ICardRepository cards, ICardNumberGenerator numberGenerator, IClock clock)
    {
        _wallets = wallets;
        _cards = cards;
        _numberGenerator = numberGenerator;
        _clock = clock;
    }

    public async Task<IssueCardResult> Handle(IssueCardCommand request, CancellationToken cancellationToken)
    {
        // Same 404 whether the wallet doesn't exist or simply isn't the caller's, as
        // GetTransactionHistoryQueryHandler/CreateOtcRequestCommandHandler do.
        var wallet = await _wallets.GetByIdAsync(request.LinkedWalletId, cancellationToken);
        if (wallet is null || wallet.OwnerId != request.OwnerId)
            throw new WalletNotFoundException(request.LinkedWalletId);

        var (pan, cvv) = _numberGenerator.Generate();
        var now = _clock.UtcNow;

        var card = new Card(
            CardId.New(), request.OwnerId, request.LinkedWalletId, wallet.Currency, request.CardholderName,
            pan, cvv, now.Month, now.Year + ValidityYears, now);
        _cards.Add(card);

        return new IssueCardResult(
            card.Id, card.LinkedWalletId, card.Currency, card.CardholderName, card.Last4,
            card.ExpiryMonth, card.ExpiryYear, card.Status, card.IssuedAt);
    }
}
