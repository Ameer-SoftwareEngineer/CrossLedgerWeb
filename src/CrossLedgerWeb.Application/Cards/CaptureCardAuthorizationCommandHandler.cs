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
/// Resolves an Open hold into a real purchase (specification 12.3/12.4's "Settlement" week) -
/// called from the issuing_transaction.created (type "capture") webhook, which Stripe sends to
/// its ordinary, non-synchronous endpoint, separately from the real-time authorization request.
/// The captured amount can be less than the hold's own Amount (a partial capture) - this posts
/// exactly the captured amount; the undrawn remainder was never debited anywhere, so it is
/// simply back in available balance the moment this hold stops being Open (see
/// DecideCardAuthorizationCommandHandler's own "available balance" calculation).
///
/// Deliberately does not handle over-capture (captured amount greater than the held Amount,
/// which Stripe explicitly permits for some merchant categories): posting more than was held
/// debits the card wallet for the full captured amount, and if that exceeds what the wallet
/// actually holds, Wallet.Debit throws InsufficientFundsException - caught here and logged as
/// an unresolved error with the hold left Open, rather than silently losing money or letting a
/// Card wallet go negative (Wallet.Debit forbids that for WalletKind.Card on purpose). A real
/// fix needs its own slice - manual ops resolution, same spirit as spec's PENDING_MANUAL state.
/// </summary>
public sealed class CaptureCardAuthorizationCommandHandler : IRequestHandler<CaptureCardAuthorizationCommand, CaptureCardAuthorizationResult>
{
    private readonly ICardRepository _cards;
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly ICardPurchaseRepository _purchases;
    private readonly IWalletRepository _wallets;
    private readonly IMerchantSettlementWalletResolver _merchantSettlementWallets;
    private readonly IClock _clock;
    private readonly ILogger<CaptureCardAuthorizationCommandHandler> _logger;

    public CaptureCardAuthorizationCommandHandler(
        ICardRepository cards,
        ICardAuthorizationRepository authorizations,
        ICardPurchaseRepository purchases,
        IWalletRepository wallets,
        IMerchantSettlementWalletResolver merchantSettlementWallets,
        IClock clock,
        ILogger<CaptureCardAuthorizationCommandHandler> logger)
    {
        _cards = cards;
        _authorizations = authorizations;
        _purchases = purchases;
        _wallets = wallets;
        _merchantSettlementWallets = merchantSettlementWallets;
        _clock = clock;
        _logger = logger;
    }

    public async Task<CaptureCardAuthorizationResult> Handle(CaptureCardAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var alreadyCaptured = await _authorizations.GetByStripeTransactionIdAsync(request.StripeTransactionId, cancellationToken);
        if (alreadyCaptured is not null)
            return new CaptureCardAuthorizationResult(Captured: true, Reason: null);

        var authorization = await _authorizations.GetByStripeAuthorizationIdAsync(request.StripeAuthorizationId, cancellationToken);
        if (authorization is null)
        {
            _logger.LogWarning(
                "Capture {StripeTransactionId} references unknown authorization {StripeAuthorizationId} - ignoring.",
                request.StripeTransactionId, request.StripeAuthorizationId);
            return new CaptureCardAuthorizationResult(Captured: false, Reason: "unknown_authorization");
        }

        if (authorization.HoldStatus != CardAuthorizationHoldStatus.Open)
        {
            _logger.LogInformation(
                "Capture {StripeTransactionId} arrived for authorization {StripeAuthorizationId} whose hold is already {HoldStatus} - ignoring, not an error.",
                request.StripeTransactionId, request.StripeAuthorizationId, authorization.HoldStatus);
            return new CaptureCardAuthorizationResult(Captured: false, Reason: "hold_not_open");
        }

        var card = await _cards.GetByIdAsync(authorization.CardId, cancellationToken)
            ?? throw new CardNotFoundException(authorization.CardId);

        Currency currency;
        try
        {
            currency = Currency.From(request.CurrencyCode);
        }
        catch (ArgumentException)
        {
            _logger.LogWarning(
                "Capture {StripeTransactionId} for authorization {StripeAuthorizationId} used an unsupported currency {CurrencyCode} - leaving the hold Open.",
                request.StripeTransactionId, request.StripeAuthorizationId, request.CurrencyCode);
            return new CaptureCardAuthorizationResult(Captured: false, Reason: "currency_mismatch");
        }

        if (currency != card.Currency)
        {
            _logger.LogWarning(
                "Capture {StripeTransactionId} for authorization {StripeAuthorizationId} requested {RequestCurrency} but the card is {CardCurrency} - leaving the hold Open.",
                request.StripeTransactionId, request.StripeAuthorizationId, currency.Code, card.Currency.Code);
            return new CaptureCardAuthorizationResult(Captured: false, Reason: "currency_mismatch");
        }

        var capturedAmount = new Money(ToDecimalAmount(request.CapturedAmountMinorUnits, currency.DecimalPlaces), currency);

        try
        {
            var wallet = await _wallets.GetByIdAsync(card.CardWalletId, cancellationToken)
                ?? throw new WalletNotFoundException(card.CardWalletId);
            var merchantSettlementWallet = await _merchantSettlementWallets.GetMerchantSettlementWalletAsync(card.Currency, cancellationToken);

            var now = _clock.UtcNow;
            var transferId = TransferId.New();
            CardPurchasePoster.Post(transferId, wallet, merchantSettlementWallet, capturedAmount, now);

            var purchase = new CardPurchase(CardPurchaseId.New(), card.Id, transferId, authorization.MerchantName, capturedAmount, now);
            _purchases.Add(purchase);

            authorization.Capture(request.StripeTransactionId, capturedAmount, transferId, now);

            return new CaptureCardAuthorizationResult(Captured: true, Reason: null);
        }
        catch (InsufficientFundsException)
        {
            _logger.LogError(
                "Capture {StripeTransactionId} for authorization {StripeAuthorizationId} wants {CapturedAmount} but the card wallet can't cover it (likely an over-capture) - leaving the hold Open for manual resolution.",
                request.StripeTransactionId, request.StripeAuthorizationId, capturedAmount);
            return new CaptureCardAuthorizationResult(Captured: false, Reason: "insufficient_funds");
        }
    }

    private static decimal ToDecimalAmount(long minorUnits, int decimalPlaces) =>
        minorUnits / (decimal)Math.Pow(10, decimalPlaces);
}
