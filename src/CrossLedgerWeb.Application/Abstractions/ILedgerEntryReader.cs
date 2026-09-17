using CrossLedgerWeb.Application.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>A transfer's four entries span up to four different wallets - crossing the
/// Wallet aggregate's own boundary (CrossLedgerWebDbContext deliberately exposes no
/// DbSet&lt;LedgerEntry&gt; - it's reached only through Wallet.Entries). Reading across
/// that boundary is exactly the set-based case specification 4's decision rule reserves
/// for Dapper rather than EF Core.</summary>
public interface ILedgerEntryReader
{
    Task<IReadOnlyList<LedgerEntryDetail>> GetByTransferIdAsync(TransferId transferId, CancellationToken cancellationToken);
}
