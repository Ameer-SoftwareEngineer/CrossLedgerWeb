using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Cards;

public sealed record SimulateCardPurchaseResult(
    CardPurchaseId PurchaseId,
    TransferId TransferId,
    string MerchantName,
    Money Amount,
    DateTimeOffset PostedAt);
