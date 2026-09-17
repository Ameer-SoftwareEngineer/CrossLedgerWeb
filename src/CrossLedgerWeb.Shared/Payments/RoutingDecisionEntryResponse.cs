namespace CrossLedgerWeb.Shared.Payments;

public sealed record RoutingDecisionEntryResponse(
    string ProviderCode, int Rank, decimal Score, decimal FeeAmount, string FeeCurrency, double EstimatedSettlementMinutes, DateTimeOffset RecordedAt);
