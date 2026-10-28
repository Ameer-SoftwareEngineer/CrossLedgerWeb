using MediatR;

namespace CrossLedgerWeb.Application.Admin;

/// <summary>Empty result means the ledger is healthy - every currency's entries sum to
/// zero (specification 2.1, 4.1's usp_ReconcileLedger).</summary>
public sealed record ReconcileLedgerQuery : IRequest<IReadOnlyList<LedgerImbalance>>;

public sealed record LedgerImbalance(string CurrencyCode, decimal NetAmount);
