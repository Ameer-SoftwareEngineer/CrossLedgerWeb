using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Cards;

public sealed record CardSummary(
    CardId CardId,
    WalletId LinkedWalletId,
    Currency Currency,
    string CardholderName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    CardStatus Status,
    decimal? SpendingLimit,
    DateTimeOffset IssuedAt);
