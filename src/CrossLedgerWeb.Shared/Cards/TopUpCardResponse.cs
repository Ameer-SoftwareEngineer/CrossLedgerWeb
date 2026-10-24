namespace CrossLedgerWeb.Shared.Cards;

public sealed record TopUpCardResponse(Guid TransferId, decimal Amount, string Currency, decimal NewCardBalance, DateTimeOffset PostedAt);
