using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Cards;

public sealed record TopUpCardResult(TransferId TransferId, Money Amount, decimal NewCardBalance, DateTimeOffset PostedAt);
