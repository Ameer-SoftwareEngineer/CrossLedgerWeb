using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>Calls dbo.usp_GetAccountStatement (versioned in the separate
/// CrossLedgerDatabase project) directly through Dapper, same as TransactionHistoryReader
/// and FxRateOhlcReader - deliberately bypassing EF Core and the Wallet aggregate for
/// this read-only, window-function-backed path.</summary>
public sealed class AccountStatementReader : IAccountStatementReader
{
    private readonly string _connectionString;

    public AccountStatementReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
    }

    public async Task<AccountStatement> GetStatementAsync(
        WalletId walletId, Currency currency, DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_GetAccountStatement",
            new { WalletId = walletId.Value, FromDate = fromDate, ToDate = toDate },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        using var results = await connection.QueryMultipleAsync(command);
        var openingBalance = await results.ReadSingleAsync<decimal>();
        var rows = (await results.ReadAsync<AccountStatementRow>()).ToList();

        var entries = rows
            .Select(row => new AccountStatementEntry(
                new LedgerEntryId(row.Id),
                new TransferId(row.TransferId),
                Enum.Parse<LedgerDirection>(row.Direction),
                new Money(row.Amount, Currency.From(row.CurrencyCode)),
                row.PostedAt,
                row.RunningBalance))
            .ToList();

        return new AccountStatement(currency, openingBalance, entries);
    }

    private sealed record AccountStatementRow(
        Guid Id, Guid TransferId, string Direction, decimal Amount, string CurrencyCode, DateTimeOffset PostedAt, decimal RunningBalance);
}
