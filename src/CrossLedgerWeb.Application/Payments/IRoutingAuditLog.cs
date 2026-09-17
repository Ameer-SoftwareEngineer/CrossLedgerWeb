using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Payments;

/// <summary>Every routing decision is persisted, full comparison and not just the
/// winner, so any transfer can be explained months later - an audit requirement in
/// regulated payments (specification 5.3).</summary>
public interface IRoutingAuditLog
{
    Task RecordAsync(TransferId transferId, IReadOnlyList<ScoredProviderQuote> rankedQuotes, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoutingDecisionEntry>> GetByTransferIdAsync(TransferId transferId, CancellationToken cancellationToken);
}
