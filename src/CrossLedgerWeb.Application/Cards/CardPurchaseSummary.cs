using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Cards;

public sealed record CardPurchaseSummary(CardPurchaseId PurchaseId, TransferId TransferId, string MerchantName, Money Amount, DateTimeOffset PostedAt);
