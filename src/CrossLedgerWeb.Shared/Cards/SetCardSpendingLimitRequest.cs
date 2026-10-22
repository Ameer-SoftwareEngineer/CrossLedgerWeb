namespace CrossLedgerWeb.Shared.Cards;

/// <summary>Null clears the limit - the card can then be used for any amount again.</summary>
public sealed record SetCardSpendingLimitRequest(decimal? Limit);
