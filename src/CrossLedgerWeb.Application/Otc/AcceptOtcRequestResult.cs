using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Otc;

public sealed record AcceptOtcRequestResult(
    TransferId TransferId,
    Money SourceAmount,
    Money TargetAmount,
    DateTimeOffset PostedAt);
