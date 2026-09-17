using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by usp_GetTransactionHistory (specification 4.1) via Dapper rather
/// than EF Core - server-side OFFSET/FETCH paging over a table that can hold millions of
/// rows belongs in SQL, not in a LINQ query that would have to pull every candidate row
/// into memory first.</summary>
public interface ITransactionHistoryReader
{
    Task<TransactionHistoryPage> GetPageAsync(
        WalletId walletId,
        int pageNumber,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken);
}
