namespace CrossLedgerWeb.Shared.Cards;

public sealed record CardResponse(
    Guid CardId,
    Guid LinkedWalletId,
    string Currency,
    string CardholderName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string Status,
    decimal? SpendingLimit,
    DateTimeOffset IssuedAt);
