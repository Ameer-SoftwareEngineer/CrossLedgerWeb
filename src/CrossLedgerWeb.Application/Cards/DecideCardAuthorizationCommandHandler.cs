using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>
/// Decides one Stripe Issuing real-time authorization request (specification 12.3) -
/// called from the issuing_authorization.request webhook, inside its 2-second budget. An
/// approval is posted as an immediate, full-amount purchase via the exact same
/// CardPurchasePoster/CardPurchase pair SimulateCardPurchaseCommandHandler already uses -
/// true hold-then-separately-capture (partial captures, an expiry sweeper for an
/// uncaptured hold) needs a pending-vs-settled ledger-entry state nothing else in this
/// codebase has yet, and is spec'd as its own later "Settlement" slice (week 18); this
/// slice's "reserve funds" is the synchronous webhook-to-ledger decision itself.
/// </summary>
public sealed class DecideCardAuthorizationCommandHandler : IRequestHandler<DecideCardAuthorizationCommand, DecideCardAuthorizationResult>
{
    private readonly ICardRepository _cards;
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly ICardPurchaseRepository _purchases;
    private readonly IWalletRepository _wallets;
    private readonly IMerchantSettlementWalletResolver _merchantSettlementWallets;
    private readonly IClock _clock;
    private readonly ILogger<DecideCardAuthorizationCommandHandler> _logger;

    public DecideCardAuthorizationCommandHandler(
        ICardRepository cards,
        ICardAuthorizationRepository authorizations,
        ICardPurchaseRepository purchases,
        IWalletRepository wallets,
        IMerchantSettlementWalletResolver merchantSettlementWallets,
        IClock clock,
        ILogger<DecideCardAuthorizationCommandHandler> logger)
    {
        _cards = cards;
        _authorizations = authorizations;
        _purchases = purchases;
        _wallets = wallets;
        _merchantSettlementWallets = merchantSettlementWallets;
        _clock = clock;
        _logger = logger;
    }

    public async Task<DecideCardAuthorizationResult> Handle(DecideCardAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _authorizations.GetByStripeAuthorizationIdAsync(request.StripeAuthorizationId, cancellationToken);
        if (existing is not null)
            return new DecideCardAuthorizationResult(existing.Approved, existing.DeclineReason);

        var card = await _cards.GetByStripeCardIdAsync(request.StripeCardId, cancellationToken);
        if (card is null)
        {
            _logger.LogWarning(
                "Issuing authorization {StripeAuthorizationId} references unknown Stripe card {StripeCardId} - declining without an audit record.",
                request.StripeAuthorizationId, request.StripeCardId);
            return new DecideCardAuthorizationResult(false, "unknown_card");
        }

        Currency currency;
        try
        {
            currency = Currency.From(request.CurrencyCode);
        }
        catch (ArgumentException)
        {
            _logger.LogWarning(
                "Issuing authorization {StripeAuthorizationId} for card {CardId} used an unsupported currency {CurrencyCode} - declining without an audit record.",
                request.StripeAuthorizationId, card.Id, request.CurrencyCode);
            return new DecideCardAuthorizationResult(false, "currency_mismatch");
        }

        if (currency != card.Currency)
        {
            _logger.LogWarning(
                "Issuing authorization {StripeAuthorizationId} for card {CardId} requested {RequestCurrency} but the card is {CardCurrency} - declining without an audit record.",
                request.StripeAuthorizationId, card.Id, currency.Code, card.Currency.Code);
            return new DecideCardAuthorizationResult(false, "currency_mismatch");
        }

        var amount = new Money(ToDecimalAmount(request.AmountMinorUnits, currency.DecimalPlaces), currency);

        string? declineReason = null;
        TransferId? transferId = null;

        try
        {
            card.EnsureCanSpend(amount);

            var wallet = await _wallets.GetByIdAsync(card.CardWalletId, cancellationToken)
                ?? throw new WalletNotFoundException(card.CardWalletId);
            var merchantSettlementWallet = await _merchantSettlementWallets.GetMerchantSettlementWalletAsync(card.Currency, cancellationToken);

            var now = _clock.UtcNow;
            var newTransferId = TransferId.New();
            CardPurchasePoster.Post(newTransferId, wallet, merchantSettlementWallet, amount, now);

            var purchase = new CardPurchase(CardPurchaseId.New(), card.Id, newTransferId, request.MerchantName, amount, now);
            _purchases.Add(purchase);
            transferId = newTransferId;
        }
        catch (InvalidCardStateException)
        {
            declineReason = "card_not_active";
        }
        catch (CardSpendingLimitExceededException)
        {
            declineReason = "spending_limit_exceeded";
        }
        catch (InsufficientFundsException)
        {
            declineReason = "insufficient_funds";
        }

        var approved = declineReason is null;
        var authorization = new CardAuthorization(
            CardAuthorizationId.New(), card.Id, request.StripeAuthorizationId, amount,
            request.MerchantName, approved, declineReason, transferId, _clock.UtcNow);
        _authorizations.Add(authorization);

        return new DecideCardAuthorizationResult(approved, declineReason);
    }

    private static decimal ToDecimalAmount(long minorUnits, int decimalPlaces) =>
        minorUnits / (decimal)Math.Pow(10, decimalPlaces);
}
