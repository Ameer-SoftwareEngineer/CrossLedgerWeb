namespace CrossLedgerWeb.Shared.Otc;

public sealed record AcceptOtcRequestResponse(
    Guid TransferId,
    decimal SourceAmount,
    string SourceCurrency,
    decimal TargetAmount,
    string TargetCurrency,
    DateTimeOffset PostedAt);
