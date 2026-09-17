using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>Calls dbo.usp_GetTransactionHistory (versioned in the separate
/// CrossLedgerDatabase project) directly through Dapper - specification 4's decision
/// rule for set-based, server-side-paged reads, deliberately bypassing EF Core and the
/// Wallet aggregate for this read-only path.</summary>
public sealed class TransactionHistoryReader : ITransactionHistoryReader
{
    private readonly string _connectionString;

    public TransactionHistoryReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
    }

    public async Task<TransactionHistoryPage> GetPageAsync(
        WalletId walletId,
        int pageNumber,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_GetTransactionHistory",
            new
            {
                WalletId = walletId.Value,
                PageNumber = pageNumber,
                PageSize = pageSize,
                FromDate = fromDate,
                ToDate = toDate,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        using var results = await connection.QueryMultipleAsync(command);
        var rows = (await results.ReadAsync<TransactionHistoryRow>()).ToList();
        var totalCount = await results.ReadSingleAsync<int>();

        var entries = rows
            .Select(row =>
            {
                var currency = Currency.From(row.CurrencyCode);
                return new TransactionHistoryEntry(
                    new LedgerEntryId(row.Id),
                    new TransferId(row.TransferId),
                    Enum.Parse<LedgerDirection>(row.Direction),
                    new Money(row.Amount, currency),
                    new Money(row.SignedAmount, currency),
                    row.PostedAt);
            })
            .ToList();

        return new TransactionHistoryPage(entries, totalCount, pageNumber, pageSize);
    }

    private sealed record TransactionHistoryRow(
        Guid Id, Guid TransferId, string Direction, decimal Amount, string CurrencyCode, DateTimeOffset PostedAt, decimal SignedAmount);
}
