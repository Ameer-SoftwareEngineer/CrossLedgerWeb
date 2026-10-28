using CrossLedgerWeb.Application.Admin;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by dbo.usp_ReconcileLedger (specification 4.1) via Dapper - pure
/// set-based aggregation over the entire ledger table, the kind of work that belongs in
/// SQL (specification section 4's decision rule), not pulled into C# to add up.</summary>
public interface ILedgerReconciliationReader
{
    Task<IReadOnlyList<LedgerImbalance>> ReconcileAsync(CancellationToken cancellationToken);
}
