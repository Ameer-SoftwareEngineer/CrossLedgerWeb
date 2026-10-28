using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>Calls dbo.usp_GetWalletBalance (versioned in the separate
/// CrossLedgerDatabase project) through Dapper, same as the other readers in this
/// folder. The proc throws 50002 WALLET_NOT_FOUND for a missing wallet; translated to
/// null here so the caller gets the same not-found shape GetByIdAsync does.</summary>
public sealed class WalletBalanceReader : IWalletBalanceReader
{
    private readonly string _connectionString;

    public WalletBalanceReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
    }

    public async Task<WalletBalance?> GetBalanceAsync(WalletId walletId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_GetWalletBalance",
            new { WalletId = walletId.Value },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        try
        {
            var row = await connection.QuerySingleAsync<Row>(command);
            return new WalletBalance(new WalletId(row.WalletId), Currency.From(row.Currency), row.Balance);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            return null;
        }
    }

    private sealed record Row(Guid WalletId, string Currency, decimal Balance);
}
