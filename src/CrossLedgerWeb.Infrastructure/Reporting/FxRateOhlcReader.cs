using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>Calls dbo.usp_GetFxRateOHLC (versioned in the separate CrossLedgerDatabase
/// project) through Dapper - specification 4's decision rule for window-function
/// aggregation with no clean LINQ equivalent. dbo.FxRateSnapshots has no scheduled
/// capture job wired up yet (specification 3.7's Week-11 Azure Functions milestone), so
/// this genuinely returns an empty list until that exists - the procedure and this
/// reader are correct now regardless of when the table starts being fed.</summary>
public sealed class FxRateOhlcReader : IFxRateOhlcReader
{
    private readonly string _connectionString;

    public FxRateOhlcReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
    }

    public async Task<IReadOnlyList<FxRateOhlcPoint>> GetDailyAsync(
        Currency from, Currency to, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_GetFxRateOHLC",
            new
            {
                FromCurrency = from.Code,
                ToCurrency = to.Code,
                FromDate = fromDate.ToDateTime(TimeOnly.MinValue),
                ToDate = toDate.ToDateTime(TimeOnly.MinValue),
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<FxRateOhlcRow>(command);

        return rows
            .Select(row => new FxRateOhlcPoint(DateOnly.FromDateTime(row.RateDate), row.Open, row.High, row.Low, row.Close))
            .ToList();
    }

    private sealed record FxRateOhlcRow(DateTime RateDate, decimal Open, decimal High, decimal Low, decimal Close);
}
