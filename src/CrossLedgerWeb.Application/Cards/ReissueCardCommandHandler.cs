using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>
/// Closes a lost/stolen/damaged card and issues its replacement on the same CardWalletId -
/// the balance carries over automatically because it's the same wallet, not a transfer.
/// Goes through the exact same ICardIssuer.IssueAsync call IssueCardCommandHandler makes
/// (real Stripe issuance or the simulated issuer, whichever CardIssuing:UseStripe selects),
/// and the new card still starts PendingApproval like any other issuance - a replacement
/// deserves the same admin review gate, not a special-cased fast path.
///
/// Known, documented gap: if the old card was Stripe-issued (StripeCardholderId set), its
/// Stripe-side card is left active rather than explicitly canceled via Stripe's own API.
/// CrossLedger's own Close() already blocks it from ever being authorized again regardless
/// of what Stripe's side thinks (DecideCardAuthorizationCommandHandler declines anything for
/// a non-Active card) - this is a cosmetic gap on Stripe's own dashboard only, not a security
/// one, same "named, not hidden" treatment every other deferred edge case in this project gets.
/// </summary>
public sealed class ReissueCardCommandHandler : IRequestHandler<ReissueCardCommand, ReissueCardResult>
{
    private readonly ICardRepository _cards;
    private readonly ICardIssuer _cardIssuer;
    private readonly IWalletRepository _wallets;
    private readonly IClock _clock;

    public ReissueCardCommandHandler(ICardRepository cards, ICardIssuer cardIssuer, IWalletRepository wallets, IClock clock)
    {
        _cards = cards;
        _cardIssuer = cardIssuer;
        _wallets = wallets;
        _clock = clock;
    }

    public async Task<ReissueCardResult> Handle(ReissueCardCommand request, CancellationToken cancellationToken)
    {
        var oldCard = await _cards.GetByIdAsync(request.OldCardId, cancellationToken);
        if (oldCard is null || oldCard.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.OldCardId);

        var now = _clock.UtcNow;
        oldCard.Close(now);

        var shippingAddress = request.NewShippingAddress ?? oldCard.ShippingAddress;

        var issued = await _cardIssuer.IssueAsync(
            new CardIssuanceRequest(oldCard.OwnerId, oldCard.CardholderName, oldCard.CardType, oldCard.Currency, oldCard.RegisteredAddress),
            cancellationToken);

        var newCard = new Card(
            CardId.New(), oldCard.OwnerId, oldCard.CardWalletId, oldCard.Currency, oldCard.CardType, oldCard.CardholderName,
            issued.Pan, issued.Cvv, issued.ExpiryMonth, issued.ExpiryYear, now,
            oldCard.SourceOfFunds, oldCard.EstimatedGrossAnnualIncome, oldCard.EstimatedNetWorth, oldCard.IsActivelyEmployed,
            oldCard.EmployerCompanyName, oldCard.JobTitle, oldCard.RegisteredAddress, shippingAddress,
            issued.StripeCardholderId, issued.StripeCardId, oldCard.Id);
        _cards.Add(newCard);

        var wallet = await _wallets.GetByIdAsync(newCard.CardWalletId, cancellationToken);
        var balance = wallet?.Balance.Amount ?? 0m;

        return new ReissueCardResult(
            newCard.Id, newCard.CardWalletId, newCard.Currency, newCard.CardType, newCard.CardholderName, newCard.Last4,
            newCard.ExpiryMonth, newCard.ExpiryYear, newCard.Status, balance, newCard.IssuedAt);
    }
}
