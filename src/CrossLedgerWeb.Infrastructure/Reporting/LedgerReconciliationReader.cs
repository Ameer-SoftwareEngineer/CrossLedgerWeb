using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>Calls dbo.usp_ReconcileLedger (versioned in the separate
/// CrossLedgerDatabase project) through Dapper, same as the other readers in this
/// folder.</summary>
public sealed class LedgerReconciliationReader : ILedgerReconciliationReader
{
    private readonly string _connectionString;

    public LedgerReconciliationReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
    }

    public async Task<IReadOnlyList<LedgerImbalance>> ReconcileAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        try
        {
            var rows = await connection.QueryAsync<LedgerImbalanceRow>(new CommandDefinition(
                "dbo.usp_ReconcileLedger", commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return rows.Select(r => new LedgerImbalance(r.CurrencyCode, r.NetAmount)).ToList();
        }
        catch (SqlException ex) when (ex.Number == 50004)
        {
            // usp_ReconcileLedger both returns the offending rows AND throws 50004 once
            // it finds any, so a scheduled caller can both log the detail and treat a
            // non-zero exit as an incident. If the driver surfaces that THROW before
            // Dapper finishes materializing the result set, re-read the same aggregation
            // directly so this still reports which currencies are unbalanced rather than
            // just the fact that something is wrong.
            var rows = await connection.QueryAsync<LedgerImbalanceRow>(new CommandDefinition(
                """
                SELECT CurrencyCode, SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END) AS NetAmount
                FROM dbo.LedgerEntries
                GROUP BY CurrencyCode
                HAVING SUM(CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END) <> 0
                """,
                cancellationToken: cancellationToken));

            return rows.Select(r => new LedgerImbalance(r.CurrencyCode, r.NetAmount)).ToList();
        }
    }

    private sealed record LedgerImbalanceRow(string CurrencyCode, decimal NetAmount);
}
