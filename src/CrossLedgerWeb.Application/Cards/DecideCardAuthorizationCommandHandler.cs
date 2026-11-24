using CrossLedgerWeb.Application.Abstractions;
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
/// Every decline reason that depends only on the card's own state or policy (frozen/
/// not-active/not-activated, spending-limit-exceeded, MCC, country, daily/monthly limits)
/// is decided here in plain C#/EF - none of those race against a concurrent request. Only
/// the final "is there enough available balance right now" decision, and the one resulting
/// CardAuthorizations insert, are delegated to ICardAuthorizationHoldReservation
/// (dbo.usp_ReserveCardAuthorizationHold) - that is the one check that previously raced
/// (two concurrent requests could both read the same available balance before either's
/// hold landed, and both approve), and the fix is the same locking usp_PostTransfer already
/// uses, not a C#-level lock. Insufficient-funds is decided last, after every other policy
/// check, so the serializable lock is only taken when nothing else would already have
/// declined the request.
/// </summary>
public sealed class DecideCardAuthorizationCommandHandler : IRequestHandler<DecideCardAuthorizationCommand, DecideCardAuthorizationResult>
{
    private readonly ICardRepository _cards;
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly ICardPurchaseRepository _purchases;
    private readonly ICardAuthorizationHoldReservation _holdReservation;
    private readonly IClock _clock;
    private readonly IOptions<CardIssuingOptions> _options;
    private readonly ILogger<DecideCardAuthorizationCommandHandler> _logger;

    public DecideCardAuthorizationCommandHandler(
        ICardRepository cards,
        ICardAuthorizationRepository authorizations,
        ICardPurchaseRepository purchases,
        ICardAuthorizationHoldReservation holdReservation,
        IClock clock,
        IOptions<CardIssuingOptions> options,
        ILogger<DecideCardAuthorizationCommandHandler> logger)
    {
        _cards = cards;
        _authorizations = authorizations;
        _purchases = purchases;
        _holdReservation = holdReservation;
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

        // Every non-financial decline is already decided - insert its audit row directly,
        // nothing was ever held so there is no race to protect against.
        if (declineReason is not null)
        {
            var declined = new CardAuthorization(
                CardAuthorizationId.New(), card.Id, request.StripeAuthorizationId, amount,
                request.MerchantName, approved: false, declineReason, holdDuration: null, _clock.UtcNow);
            _authorizations.Add(declined);
            return new DecideCardAuthorizationResult(false, declineReason);
        }

        // Only the balance-vs-open-holds decision reaches here - the one check that
        // actually races against a concurrent request. usp_ReserveCardAuthorizationHold
        // makes the decision and inserts the resulting audit row atomically, under lock.
        var decidedAt = _clock.UtcNow;
        var expiresAtIfApproved = decidedAt + TimeSpan.FromDays(_options.Value.AuthorizationHoldDays);
        var reservation = await _holdReservation.ReserveAsync(
            CardAuthorizationId.New(), card.Id, card.CardWalletId, request.StripeAuthorizationId,
            request.MerchantName, amount, decidedAt, expiresAtIfApproved, cancellationToken);

        return new DecideCardAuthorizationResult(reservation.Approved, reservation.DeclineReason);
    }

    private static decimal ToDecimalAmount(long minorUnits, int decimalPlaces) =>
        minorUnits / (decimal)Math.Pow(10, decimalPlaces);
}
