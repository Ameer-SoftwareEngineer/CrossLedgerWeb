using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Cards;

/// <summary>
/// A record of one simulated card purchase - the actual money movement is two
/// LedgerEntries (CardPurchasePoster.Post) tagged with <see cref="TransferId"/>, same as
/// any other transfer; this entity exists only so a card's own purchase history can show
/// a merchant name and be queried per-card without joining back through LedgerEntries.
/// No lifecycle of its own - once posted, a purchase is immutable.
/// </summary>
public sealed class CardPurchase
{
    public CardPurchaseId Id { get; }
    public CardId CardId { get; }
    public TransferId TransferId { get; }
    public string MerchantName { get; }
    public Money Amount { get; }
    public DateTimeOffset PostedAt { get; }

    public CardPurchase(CardPurchaseId id, CardId cardId, TransferId transferId, string merchantName, Money amount, DateTimeOffset postedAt)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            throw new ArgumentException("A card purchase requires a merchant name.", nameof(merchantName));

        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount.Amount, "A card purchase amount must be positive.");

        Id = id;
        CardId = cardId;
        TransferId = transferId;
        MerchantName = merchantName;
        Amount = amount;
        PostedAt = postedAt;
    }

    /// <summary>Rehydration constructor for EF Core materialization: Amount maps as a
    /// complex property, which EF Core cannot bind through a constructor parameter, so
    /// this constructor omits it and EF sets it directly afterward - same pattern as
    /// LedgerEntry's own rehydration constructor.</summary>
    private CardPurchase(CardPurchaseId id, CardId cardId, TransferId transferId, string merchantName, DateTimeOffset postedAt)
    {
        Id = id;
        CardId = cardId;
        TransferId = transferId;
        MerchantName = merchantName;
        PostedAt = postedAt;
        Amount = default;
    }
}
