using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>One combined command for the MCC block list, country allow-list, and the two
/// rolling-window spend caps - see Card.SetSpendingControls. Distinct from
/// SetCardSpendingLimitCommand, which stays scoped to the original per-purchase cap.
/// Empty sets mean "no restriction," null limits mean "uncapped" - same conventions the
/// per-purchase limit already uses.</summary>
public sealed record SetCardSpendingControlsCommand(
    CardId CardId,
    UserId CallerId,
    IReadOnlySet<string> BlockedMerchantCategoryCodes,
    IReadOnlySet<string> AllowedCountries,
    decimal? DailyLimit,
    decimal? MonthlyLimit) : IRequest;
