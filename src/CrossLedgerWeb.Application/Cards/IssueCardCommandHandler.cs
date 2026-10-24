using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Issues a dev-simulated prepaid card - no real card network or issuing
/// processor involved, same spirit as DevLogSmsSender simulating an SMS gateway. A fresh
/// Card-kind wallet is created alongside it, starting at zero; TopUpCardCommand is the
/// only way money ever enters it.</summary>
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
        var currency = Currency.From(request.Currency);
        var now = _clock.UtcNow;

        var cardWallet = new Wallet(WalletId.New(), request.OwnerId, currency, WalletKind.Card);
        _wallets.Add(cardWallet);

        var (pan, cvv) = _numberGenerator.Generate();
        var shippingAddress = request.ShippingAddress ?? request.RegisteredAddress;

        var card = new Card(
            CardId.New(), request.OwnerId, cardWallet.Id, currency, request.CardType, request.CardholderName,
            pan, cvv, now.Month, now.Year + ValidityYears, now,
            request.SourceOfFunds, request.EstimatedGrossAnnualIncome, request.EstimatedNetWorth, request.IsActivelyEmployed,
            request.EmployerCompanyName, request.JobTitle, request.RegisteredAddress, shippingAddress);
        _cards.Add(card);

        return new IssueCardResult(
            card.Id, card.CardWalletId, card.Currency, card.CardType, card.CardholderName, card.Last4,
            card.ExpiryMonth, card.ExpiryYear, card.Status, cardWallet.Balance.Amount, card.IssuedAt);
    }
}
