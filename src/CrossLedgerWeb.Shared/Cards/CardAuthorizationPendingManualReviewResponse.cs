namespace CrossLedgerWeb.Shared.Cards;

public sealed record CardAuthorizationPendingManualReviewResponse(
    Guid CardAuthorizationId,
    Guid CardId,
    string CardholderName,
    string MerchantName,
    decimal Amount,
    string CurrencyCode,
    decimal AttemptedCaptureAmount,
    string ManualReviewReason,
    DateTimeOffset DecidedAt,
    DateTimeOffset ManualReviewFlaggedAt);
