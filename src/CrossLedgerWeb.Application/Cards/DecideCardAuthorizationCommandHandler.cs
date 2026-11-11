using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>
/// Decides one Stripe Issuing real-time authorization request (specification 12.3) - called
/// from the issuing_authorization.request webhook, inside its 2-second budget. An approval
/// only opens a hold (CardAuthorization's own HoldStatus.Open, see its doc comment) - no
/// LedgerEntry is posted here. The actual purchase is posted later, by
/// CaptureCardAuthorizationCommandHandler, when Stripe's separate capture webhook arrives
/// (possibly for less than the held amount - a partial capture) - or never, if the expiry
/// sweeper releases an uncaptured hold first.
///
/// Known, unaddressed race: "available balance" below is an ordinary read (wallet balance
/// minus the sum of this card's other Open holds), not locked the way usp_PostTransfer locks
/// a wallet row under UPDLOCK/ROWLOCK for a transfer. Two authorization requests against the
/// same card arriving close enough together can both read the same available balance before
/// either's hold is inserted, and both approve - a genuine TOCTOU double-hold. Closing this
/// needs either a serializable transaction around "sum Open holds + insert" or a dedicated
/// stored procedure with the same locking usp_PostTransfer already uses; this slice documents
/// the gap rather than closing it.
/// </summary>
public sealed class DecideCardAuthorizationCommandHandler : IRequestHandler<DecideCardAuthorizationCommand, DecideCardAuthorizationResult>
{
    private readonly ICardRepository _cards;
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly ICardPurchaseRepository _purchases;
    private readonly IWalletRepository _wallets;
    private readonly IClock _clock;
    private readonly IOptions<CardIssuingOptions> _options;
    private readonly ILogger<DecideCardAuthorizationCommandHandler> _logger;

    public DecideCardAuthorizationCommandHandler(
        ICardRepository cards,
        ICardAuthorizationRepository authorizations,
        ICardPurchaseRepository purchases,
        IWalletRepository wallets,
        IClock clock,
        IOptions<CardIssuingOptions> options,
        ILogger<DecideCardAuthorizationCommandHandler> logger)
    {
        _cards = cards;
        _authorizations = authorizations;
        _purchases = purchases;
        _wallets = wallets;
        _clock = clock;
        _options = options;
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

        try
        {
            card.EnsureCanSpend(amount);

            var wallet = await _wallets.GetByIdAsync(card.CardWalletId, cancellationToken)
                ?? throw new WalletNotFoundException(card.CardWalletId);
            var openHoldTotal = await _authorizations.GetOpenHoldTotalAsync(card.Id, card.Currency, cancellationToken);
            var available = wallet.Balance - openHoldTotal;

            if (available.Amount < amount.Amount)
                throw new InsufficientFundsException(wallet.Id, available, amount);
        }
        catch (InvalidCardStateException)
        {
            declineReason = "card_not_active";
        }
        catch (CardNotActivatedException)
        {
            declineReason = "card_not_activated";
        }
        catch (CardSpendingLimitExceededException)
        {
            declineReason = "spending_limit_exceeded";
        }
        catch (InsufficientFundsException)
        {
            declineReason = "insufficient_funds";
        }

        // Policy checks beyond Card's own invariants - these need the current
        // authorization attempt's merchant data and purchase history, neither of which
        // Card.EnsureCanSpend has access to, so they live here rather than as thrown
        // domain exceptions.
        if (declineReason is null && request.MerchantCategoryCode is { } mcc && card.BlockedMerchantCategoryCodes.Contains(mcc))
        {
            declineReason = "card_category_blocked";
        }

        if (declineReason is null && request.MerchantCountry is { } country && card.AllowedCountries.Count > 0 && !card.AllowedCountries.Contains(country))
        {
            declineReason = "country_not_allowed";
        }

        if (declineReason is null && card.DailySpendingLimit is { } dailyLimit)
        {
            var spentToday = await _purchases.GetSpendTotalAsync(card.Id, _clock.UtcNow - TimeSpan.FromHours(24), cancellationToken);
            if (spentToday + amount.Amount > dailyLimit)
                declineReason = "daily_limit_exceeded";
        }

        if (declineReason is null && card.MonthlySpendingLimit is { } monthlyLimit)
        {
            var spentThisMonth = await _purchases.GetSpendTotalAsync(card.Id, _clock.UtcNow - TimeSpan.FromDays(30), cancellationToken);
            if (spentThisMonth + amount.Amount > monthlyLimit)
                declineReason = "monthly_limit_exceeded";
        }

        var approved = declineReason is null;
        var holdDuration = approved ? TimeSpan.FromDays(_options.Value.AuthorizationHoldDays) : (TimeSpan?)null;
        var authorization = new CardAuthorization(
            CardAuthorizationId.New(), card.Id, request.StripeAuthorizationId, amount,
            request.MerchantName, approved, declineReason, holdDuration, _clock.UtcNow);
        _authorizations.Add(authorization);

        return new DecideCardAuthorizationResult(approved, declineReason);
    }

    private static decimal ToDecimalAmount(long minorUnits, int decimalPlaces) =>
        minorUnits / (decimal)Math.Pow(10, decimalPlaces);
}
