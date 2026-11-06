using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Cards;

/// <summary>
/// An audit record of one real-time authorization decision (specification 12.3) - every
/// time Stripe's issuing_authorization.request webhook asks "approve or decline?", one of
/// these is written, whichever way the decision went. A decline is terminal
/// (<see cref="HoldStatus"/> stays <see cref="CardAuthorizationHoldStatus.NotApplicable"/>) -
/// nothing was ever held, there is nothing to point a <see cref="TransferId"/> at. An
/// approval starts a hold (<see cref="CardAuthorizationHoldStatus.Open"/>, see that enum's own
/// doc comment for why a hold never touches the ledger by itself) that later resolves to
/// either <see cref="Capture"/> - a real purchase posted via CardPurchasePoster, which is when
/// <see cref="TransferId"/> finally gets set - or <see cref="Release"/>, if it times out
/// uncaptured. <see cref="CapturedAmount"/> can be less than <see cref="Amount"/> (a partial
/// capture); the undrawn remainder was never posted anywhere, so releasing it is implicit.
/// </summary>
public sealed class CardAuthorization
{
    public CardAuthorizationId Id { get; }
    public CardId CardId { get; }
    public string StripeAuthorizationId { get; }
    public Money Amount { get; }
    public string MerchantName { get; }
    public bool Approved { get; }
    public string? DeclineReason { get; }
    public CardAuthorizationHoldStatus HoldStatus { get; private set; }
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>Always in Amount's own currency - a capture never changes currency, so
    /// there is no separate currency column to keep in sync (and no nullable complex
    /// property to map - EF Core 8 has no clean story for an optional Money on a single
    /// entity that already has a required one).</summary>
    public decimal? CapturedAmount { get; private set; }
    public string? StripeTransactionId { get; private set; }
    public TransferId? TransferId { get; private set; }
    public DateTimeOffset DecidedAt { get; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>holdDuration is ignored (and must be null) for a decline - nothing is ever
    /// held, so there is nothing to expire.</summary>
    public CardAuthorization(
        CardAuthorizationId id,
        CardId cardId,
        string stripeAuthorizationId,
        Money amount,
        string merchantName,
        bool approved,
        string? declineReason,
        TimeSpan? holdDuration,
        DateTimeOffset decidedAt)
    {
        if (string.IsNullOrWhiteSpace(stripeAuthorizationId))
            throw new ArgumentException("A card authorization requires Stripe's own authorization id.", nameof(stripeAuthorizationId));

        if (string.IsNullOrWhiteSpace(merchantName))
            throw new ArgumentException("A card authorization requires a merchant name.", nameof(merchantName));

        if (approved && (declineReason is not null || holdDuration is null))
            throw new ArgumentException("An approved authorization must carry a hold duration and no decline reason.", nameof(approved));

        if (!approved && (declineReason is null || holdDuration is not null))
            throw new ArgumentException("A declined authorization must carry a decline reason and no hold duration.", nameof(approved));

        Id = id;
        CardId = cardId;
        StripeAuthorizationId = stripeAuthorizationId;
        Amount = amount;
        MerchantName = merchantName;
        Approved = approved;
        DeclineReason = declineReason;
        DecidedAt = decidedAt;
        HoldStatus = approved ? CardAuthorizationHoldStatus.Open : CardAuthorizationHoldStatus.NotApplicable;
        ExpiresAt = approved ? decidedAt + holdDuration : null;
    }

    /// <summary>Rehydration constructor for EF Core materialization: Amount maps as a
    /// complex property, which EF Core cannot bind through a constructor parameter, so this
    /// constructor omits it and EF sets it directly afterward - same pattern as
    /// CardPurchase's own rehydration constructor. CapturedAmount is a plain nullable
    /// scalar, so (like TransferId) it is bound normally.</summary>
    private CardAuthorization(
        CardAuthorizationId id,
        CardId cardId,
        string stripeAuthorizationId,
        string merchantName,
        bool approved,
        string? declineReason,
        CardAuthorizationHoldStatus holdStatus,
        DateTimeOffset? expiresAt,
        decimal? capturedAmount,
        string? stripeTransactionId,
        TransferId? transferId,
        DateTimeOffset decidedAt,
        DateTimeOffset? resolvedAt)
    {
        Id = id;
        CardId = cardId;
        StripeAuthorizationId = stripeAuthorizationId;
        MerchantName = merchantName;
        Approved = approved;
        DeclineReason = declineReason;
        HoldStatus = holdStatus;
        ExpiresAt = expiresAt;
        CapturedAmount = capturedAmount;
        StripeTransactionId = stripeTransactionId;
        TransferId = transferId;
        DecidedAt = decidedAt;
        ResolvedAt = resolvedAt;
        Amount = default;
    }

    /// <summary>Resolves an Open hold into a real purchase - capturedAmount may be less than
    /// Amount (partial capture); the gap is simply never posted anywhere, which is what
    /// frees it back into available balance with no further bookkeeping. capturedAmount must
    /// already be in Amount's own currency - the caller (CaptureCardAuthorizationCommandHandler)
    /// checks that before ever reaching here, the same way DecideCardAuthorizationCommandHandler
    /// checks the original authorization's currency.</summary>
    public void Capture(string stripeTransactionId, Money capturedAmount, TransferId transferId, DateTimeOffset capturedAt)
    {
        if (HoldStatus != CardAuthorizationHoldStatus.Open)
            throw new InvalidCardAuthorizationHoldStateException(Id, HoldStatus, "capture");

        HoldStatus = CardAuthorizationHoldStatus.Captured;
        CapturedAmount = capturedAmount.Amount;
        StripeTransactionId = stripeTransactionId;
        TransferId = transferId;
        ResolvedAt = capturedAt;
    }

    /// <summary>Resolves an Open hold with no capture ever arriving (the expiry sweeper's
    /// only job) - no ledger entry ever existed for it, so there is nothing to reverse.</summary>
    public void Release(DateTimeOffset releasedAt)
    {
        if (HoldStatus != CardAuthorizationHoldStatus.Open)
            throw new InvalidCardAuthorizationHoldStateException(Id, HoldStatus, "release");

        HoldStatus = CardAuthorizationHoldStatus.Released;
        ResolvedAt = releasedAt;
    }
}
