namespace CrossLedgerWeb.Shared.Cards;

public sealed record CardResponse(
    Guid CardId,
    Guid CardWalletId,
    string Currency,
    string CardType,
    string CardholderName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string Status,
    decimal? SpendingLimit,
    decimal Balance,
    DateTimeOffset IssuedAt);
