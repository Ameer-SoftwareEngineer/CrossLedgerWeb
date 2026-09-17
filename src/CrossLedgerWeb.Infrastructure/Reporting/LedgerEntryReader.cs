using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Ledger;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>An ad-hoc parameterised query rather than a stored procedure - unlike
/// usp_GetTransactionHistory's paged, filtered scan, this always returns a transfer's
/// small, fixed set of entries (at most four), so there's no execution-plan or paging
/// concern a procedure would earn its keep on. Still through Dapper, not EF Core: the
/// read crosses the Wallet aggregate boundary (see ILedgerEntryReader).</summary>
public sealed class LedgerEntryReader : ILedgerEntryReader
{
    private const string Sql = """
        SELECT Id, TransferId, WalletId, Direction, Amount, CurrencyCode, PostedAt,
               CASE Direction WHEN 'Credit' THEN Amount WHEN 'Debit' THEN -Amount END AS SignedAmount
        FROM dbo.LedgerEntries
        WHERE TransferId = @TransferId
        ORDER BY PostedAt ASC, Id ASC;
        """;

    private readonly string _connectionString;

    public LedgerEntryReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
    }

    public async Task<IReadOnlyList<LedgerEntryDetail>> GetByTransferIdAsync(TransferId transferId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(Sql, new { TransferId = transferId.Value }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<LedgerEntryRow>(command);

        return rows
            .Select(row =>
            {
                var currency = Currency.From(row.CurrencyCode);
                return new LedgerEntryDetail(
                    new LedgerEntryId(row.Id),
                    new TransferId(row.TransferId),
                    new WalletId(row.WalletId),
                    Enum.Parse<LedgerDirection>(row.Direction),
                    new Money(row.Amount, currency),
                    new Money(row.SignedAmount, currency),
                    row.PostedAt);
            })
            .ToList();
    }

    private sealed record LedgerEntryRow(
        Guid Id, Guid TransferId, Guid WalletId, string Direction, decimal Amount, string CurrencyCode, DateTimeOffset PostedAt, decimal SignedAmount);
}
