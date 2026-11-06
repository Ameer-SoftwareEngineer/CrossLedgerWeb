namespace CrossLedgerWeb.Application.Cards;

public sealed record DecideCardAuthorizationResult(bool Approved, string? DeclineReason);
