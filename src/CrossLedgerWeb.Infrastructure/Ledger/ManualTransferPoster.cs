using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Ledger;

/// <summary>Calls dbo.usp_PostTransfer (versioned in the separate CrossLedgerDatabase
/// project) through Dapper - a write, so it lives under Ledger rather than Reporting
/// alongside the read-only stored-proc callers. Backs the admin "manual transfer" tool
/// only; the regular customer transfer flow keeps posting through the EF-based
/// TransferPoster.</summary>
public sealed class ManualTransferPoster : IManualTransferPoster
{
    private readonly string _connectionString;
    private readonly IWalletBalanceReader _balances;

    public ManualTransferPoster(IConfiguration configuration, IWalletBalanceReader balances)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
        _balances = balances;
    }

    public async Task<ManualTransferResult> PostAsync(
        TransferId transferId,
        WalletId sourceWalletId,
        WalletId fxSettlementSourceWalletId,
        WalletId fxSettlementTargetWalletId,
        WalletId targetWalletId,
        decimal sourceAmount,
        Currency sourceCurrency,
        decimal targetAmount,
        Currency targetCurrency,
        DateTimeOffset postedAt,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_PostTransfer",
            new
            {
                TransferId = transferId.Value,
                SourceWalletId = sourceWalletId.Value,
                FxSettlementSourceWalletId = fxSettlementSourceWalletId.Value,
                FxSettlementTargetWalletId = fxSettlementTargetWalletId.Value,
                TargetWalletId = targetWalletId.Value,
                SourceAmount = sourceAmount,
                SourceCurrencyCode = sourceCurrency.Code,
                TargetAmount = targetAmount,
                TargetCurrencyCode = targetCurrency.Code,
                PostedAt = postedAt,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        try
        {
            var row = await connection.QuerySingleAsync<Row>(command);
            return new ManualTransferResult(new TransferId(row.TransferId), row.Status, row.PostedAt);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            // The proc's own error carries no balance figures, only the code - re-read
            // the source wallet's current balance (same connection pool, cheap) so the
            // exception the caller sees is as informative as the EF path's.
            var balance = await _balances.GetBalanceAsync(sourceWalletId, cancellationToken);
            throw new InsufficientFundsException(
                sourceWalletId,
                new Money(balance?.Balance ?? 0m, sourceCurrency),
                new Money(sourceAmount, sourceCurrency));
        }
        catch (SqlException ex) when (ex.Number == 50003)
        {
            throw new CurrencyMismatchException(sourceCurrency, targetCurrency);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            // Unreachable in practice - the handler already resolves all four wallets
            // (via GetOwnershipAsync / IFxSettlementWalletResolver) before calling this,
            // so this only fires on a genuine race with a concurrent delete. No wallet id
            // in the error itself to pin down which one, so the source is the best guess.
            throw new WalletNotFoundException(sourceWalletId);
        }
    }

    private sealed record Row(Guid TransferId, string Status, DateTimeOffset PostedAt);
}
