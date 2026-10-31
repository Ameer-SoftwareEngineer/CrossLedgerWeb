using System.Data;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>Calls dbo.usp_GetProviderStats (versioned in the separate CrossLedgerDatabase
/// project) through Dapper, same shape as TransferLimitsReader. Replaces
/// DefaultProviderStatsProvider's always-full-marks placeholder now that Payouts carries
/// enough history (ProviderCode and UpdatedAt) to compute specification 5.3's two
/// historical signals for real. The daily volume limit is configuration
/// (Limits:ProviderDailyVolumeLimit), the same way Limits:RollingTransferLimitPerDay is
/// configuration rather than a database row - usp_GetProviderStats takes it as a parameter
/// rather than owning it.</summary>
public sealed class ProviderStatsReader : IProviderStatsProvider
{
    private readonly string _connectionString;
    private readonly decimal _dailyVolumeLimit;

    public ProviderStatsReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
        _dailyVolumeLimit = configuration.GetValue<decimal?>("Limits:ProviderDailyVolumeLimit")
            ?? throw new InvalidOperationException("Missing 'Limits:ProviderDailyVolumeLimit' setting.");
    }

    public async Task<ProviderStats> GetStatsAsync(ProviderCode providerCode, Currency currency, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_GetProviderStats",
            new { ProviderCode = providerCode.ToString(), CurrencyCode = currency.Code, DailyVolumeLimit = _dailyVolumeLimit },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleAsync<Row>(command);

        return new ProviderStats(row.RollingSuccessRate, row.RemainingDailyHeadroomRatio);
    }

    private sealed record Row(decimal RollingSuccessRate, decimal RemainingDailyHeadroomRatio);
}
