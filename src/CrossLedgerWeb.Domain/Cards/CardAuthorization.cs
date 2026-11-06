using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Cards;

/// <summary>
/// An audit record of one real-time authorization decision (specification 12.3) - every
/// time Stripe's issuing_authorization.request webhook asks "approve or decline?", one of
/// these is written, whichever way the decision went. Same relationship to the ledger that
/// <see cref="CardPurchase"/> has: the money movement, when there is any, is posted as real
/// LedgerEntries tagged with <see cref="TransferId"/> by CardPurchasePoster - this entity
/// exists so the decision itself (including a decline's reason, which never touches the
/// ledger) can be queried per-card without inventing a ledger concept for "nothing
/// happened". <see cref="TransferId"/> is null on a decline - there is nothing to point at.
/// No lifecycle of its own - once decided, immutable, same as CardPurchase.
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
    public TransferId? TransferId { get; }
    public DateTimeOffset DecidedAt { get; }

    public CardAuthorization(
        CardAuthorizationId id,
        CardId cardId,
        string stripeAuthorizationId,
        Money amount,
        string merchantName,
        bool approved,
        string? declineReason,
        TransferId? transferId,
        DateTimeOffset decidedAt)
    {
        if (string.IsNullOrWhiteSpace(stripeAuthorizationId))
            throw new ArgumentException("A card authorization requires Stripe's own authorization id.", nameof(stripeAuthorizationId));

        if (string.IsNullOrWhiteSpace(merchantName))
            throw new ArgumentException("A card authorization requires a merchant name.", nameof(merchantName));

        if (approved && (declineReason is not null || transferId is null))
            throw new ArgumentException("An approved authorization must carry a TransferId and no decline reason.", nameof(approved));

        if (!approved && (declineReason is null || transferId is not null))
            throw new ArgumentException("A declined authorization must carry a decline reason and no TransferId.", nameof(approved));

        Id = id;
        CardId = cardId;
        StripeAuthorizationId = stripeAuthorizationId;
        Amount = amount;
        MerchantName = merchantName;
        Approved = approved;
        DeclineReason = declineReason;
        TransferId = transferId;
        DecidedAt = decidedAt;
    }

    /// <summary>Rehydration constructor for EF Core materialization: Amount maps as a
    /// complex property, which EF Core cannot bind through a constructor parameter, so
    /// this constructor omits it and EF sets it directly afterward - same pattern as
    /// CardPurchase's own rehydration constructor.</summary>
    private CardAuthorization(
        CardAuthorizationId id,
        CardId cardId,
        string stripeAuthorizationId,
        string merchantName,
        bool approved,
        string? declineReason,
        TransferId? transferId,
        DateTimeOffset decidedAt)
    {
        Id = id;
        CardId = cardId;
        StripeAuthorizationId = stripeAuthorizationId;
        MerchantName = merchantName;
        Approved = approved;
        DeclineReason = declineReason;
        TransferId = transferId;
        DecidedAt = decidedAt;
        Amount = default;
    }
}
