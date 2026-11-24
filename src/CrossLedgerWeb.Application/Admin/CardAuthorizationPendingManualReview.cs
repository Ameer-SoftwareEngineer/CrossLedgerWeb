using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Admin;

public sealed record CardAuthorizationPendingManualReview(
    CardAuthorizationId CardAuthorizationId,
    CardId CardId,
    string CardholderName,
    string MerchantName,
    decimal Amount,
    string CurrencyCode,
    decimal AttemptedCaptureAmount,
    string ManualReviewReason,
    DateTimeOffset DecidedAt,
    DateTimeOffset ManualReviewFlaggedAt);
