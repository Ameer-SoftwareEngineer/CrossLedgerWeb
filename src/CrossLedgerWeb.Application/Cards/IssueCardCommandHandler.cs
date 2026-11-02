using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Issues a card - no real card network sits behind this by default
/// (SimulatedCardIssuer), or a real Stripe Issuing Cardholder/Card when
/// CardIssuing:UseStripe is on (StripeCardIssuer) - same spirit as DevLogSmsSender
/// simulating an SMS gateway until a real one is configured. A fresh Card-kind wallet is
/// created alongside it, starting at zero; TopUpCardCommand is the only way money ever
/// enters it.</summary>
public sealed class IssueCardCommandHandler : IRequestHandler<IssueCardCommand, IssueCardResult>
{
    private readonly IWalletRepository _wallets;
    private readonly ICardRepository _cards;
    private readonly ICardIssuer _cardIssuer;
    private readonly IClock _clock;

    public IssueCardCommandHandler(IWalletRepository wallets, ICardRepository cards, ICardIssuer cardIssuer, IClock clock)
    {
        _wallets = wallets;
        _cards = cards;
        _cardIssuer = cardIssuer;
        _clock = clock;
    }

    public async Task<IssueCardResult> Handle(IssueCardCommand request, CancellationToken cancellationToken)
    {
        var currency = Currency.From(request.Currency);
        var now = _clock.UtcNow;

        var cardWallet = new Wallet(WalletId.New(), request.OwnerId, currency, WalletKind.Card);
        _wallets.Add(cardWallet);

        var issued = await _cardIssuer.IssueAsync(
            new CardIssuanceRequest(request.OwnerId, request.CardholderName, request.CardType, currency, request.RegisteredAddress),
            cancellationToken);
        var shippingAddress = request.ShippingAddress ?? request.RegisteredAddress;

        var card = new Card(
            CardId.New(), request.OwnerId, cardWallet.Id, currency, request.CardType, request.CardholderName,
            issued.Pan, issued.Cvv, issued.ExpiryMonth, issued.ExpiryYear, now,
            request.SourceOfFunds, request.EstimatedGrossAnnualIncome, request.EstimatedNetWorth, request.IsActivelyEmployed,
            request.EmployerCompanyName, request.JobTitle, request.RegisteredAddress, shippingAddress,
            issued.StripeCardholderId, issued.StripeCardId);
        _cards.Add(card);

        return new IssueCardResult(
            card.Id, card.CardWalletId, card.Currency, card.CardType, card.CardholderName, card.Last4,
            card.ExpiryMonth, card.ExpiryYear, card.Status, cardWallet.Balance.Amount, card.IssuedAt);
    }
}
