using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Payments;

/// <summary>One provider's place in a payout's routing comparison (specification 9's
/// Routing Inspector) - Rank 0 is the winner actually used; 1+ are the ordered
/// fallbacks that were considered but not needed.</summary>
public sealed record RoutingDecisionEntry(
    string ProviderCode, int Rank, decimal Score, Money Fee, double EstimatedSettlementMinutes, DateTimeOffset RecordedAt);
