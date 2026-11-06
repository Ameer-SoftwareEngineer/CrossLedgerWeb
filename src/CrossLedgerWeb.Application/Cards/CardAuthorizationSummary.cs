using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Cards;

public sealed record CardAuthorizationSummary(
    CardAuthorizationId AuthorizationId, string MerchantName, Money Amount, bool Approved, string? DeclineReason, DateTimeOffset DecidedAt);
