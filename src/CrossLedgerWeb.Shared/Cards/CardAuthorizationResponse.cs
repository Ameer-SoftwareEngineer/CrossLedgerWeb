namespace CrossLedgerWeb.Shared.Cards;

public sealed record CardAuthorizationResponse(
    Guid AuthorizationId, string MerchantName, decimal Amount, string Currency, bool Approved, string? DeclineReason, DateTimeOffset DecidedAt);
