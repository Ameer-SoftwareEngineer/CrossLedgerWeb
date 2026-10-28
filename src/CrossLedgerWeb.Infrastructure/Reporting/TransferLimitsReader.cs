using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Reporting;

/// <summary>Calls dbo.usp_CheckTransferLimits (versioned in the separate
/// CrossLedgerDatabase project) through Dapper, same as the other readers in this
/// folder. The rolling limit itself is configuration (Limits:RollingTransferLimitPerDay),
/// the same way Limits:StepUpAbove is read from configuration rather than a database
/// row today - usp_CheckTransferLimits takes it as a parameter rather than owning it.</summary>
public sealed class TransferLimitsReader : ITransferLimitsReader
{
    private readonly string _connectionString;
    private readonly decimal _rollingLimit;

    public TransferLimitsReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
        _rollingLimit = configuration.GetValue<decimal?>("Limits:RollingTransferLimitPerDay")
            ?? throw new InvalidOperationException("Missing 'Limits:RollingTransferLimitPerDay' setting.");
    }

    public async Task<TransferLimitCheck> CheckAsync(UserId ownerId, Currency currency, decimal proposedAmount, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_CheckTransferLimits",
            new { OwnerId = ownerId.Value, CurrencyCode = currency.Code, ProposedAmount = proposedAmount, RollingLimit = _rollingLimit },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleAsync<Row>(command);

        return new TransferLimitCheck(row.RollingTotal, row.ProjectedTotal, row.RollingLimit, row.ExceedsLimit);
    }

    private sealed record Row(decimal RollingTotal, decimal ProjectedTotal, decimal RollingLimit, bool ExceedsLimit);
}
