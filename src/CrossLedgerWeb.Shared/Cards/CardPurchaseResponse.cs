namespace CrossLedgerWeb.Shared.Cards;

public sealed record CardPurchaseResponse(Guid PurchaseId, Guid TransferId, string MerchantName, decimal Amount, string Currency, DateTimeOffset PostedAt);
