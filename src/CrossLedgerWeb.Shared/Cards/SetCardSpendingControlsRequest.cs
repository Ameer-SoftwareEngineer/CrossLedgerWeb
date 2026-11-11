namespace CrossLedgerWeb.Shared.Cards;

/// <summary>Empty lists mean "no restriction," null limits mean "uncapped" - same
/// conventions SetCardSpendingLimitRequest's per-purchase limit already uses.</summary>
public sealed record SetCardSpendingControlsRequest(
    IReadOnlyList<string> BlockedMerchantCategoryCodes,
    IReadOnlyList<string> AllowedCountries,
    decimal? DailyLimit,
    decimal? MonthlyLimit);
