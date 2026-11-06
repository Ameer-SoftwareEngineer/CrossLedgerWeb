namespace CrossLedgerWeb.Application.Cards;

public sealed record CaptureCardAuthorizationResult(bool Captured, string? Reason);
