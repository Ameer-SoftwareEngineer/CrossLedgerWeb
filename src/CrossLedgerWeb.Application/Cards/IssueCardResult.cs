using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Cards;

public sealed record IssueCardResult(
    CardId CardId,
    WalletId CardWalletId,
    Currency Currency,
    CardType CardType,
    string CardholderName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    CardStatus Status,
    decimal Balance,
    DateTimeOffset IssuedAt);
