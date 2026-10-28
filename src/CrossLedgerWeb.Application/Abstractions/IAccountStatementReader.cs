using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by dbo.usp_GetAccountStatement (specification 4.1) via Dapper - a
/// running balance via SUM() OVER (ORDER BY PostedAt) "cannot be expressed cleanly
/// through LINQ" (specification section 4).</summary>
public interface IAccountStatementReader
{
    Task<AccountStatement> GetStatementAsync(
        WalletId walletId, Currency currency, DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken);
}
