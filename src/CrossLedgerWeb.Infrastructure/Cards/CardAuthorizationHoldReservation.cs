using System.Data;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CrossLedgerWeb.Infrastructure.Cards;

/// <summary>Calls dbo.usp_ReserveCardAuthorizationHold (source in
/// db/procedures/usp_ReserveCardAuthorizationHold.sql) through Dapper, same shape as
/// ManualTransferPoster - a write, so it lives under Infrastructure/Cards rather than
/// Reporting.</summary>
public sealed class CardAuthorizationHoldReservation : ICardAuthorizationHoldReservation
{
    private readonly string _connectionString;

    public CardAuthorizationHoldReservation(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");
    }

    public async Task<CardAuthorizationHoldReservationResult> ReserveAsync(
        CardAuthorizationId cardAuthorizationId,
        CardId cardId,
        WalletId walletId,
        string stripeAuthorizationId,
        string merchantName,
        Money amount,
        DateTimeOffset decidedAt,
        DateTimeOffset expiresAtIfApproved,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "dbo.usp_ReserveCardAuthorizationHold",
            new
            {
                CardAuthorizationId = cardAuthorizationId.Value,
                CardId = cardId.Value,
                WalletId = walletId.Value,
                StripeAuthorizationId = stripeAuthorizationId,
                MerchantName = merchantName,
                Amount = amount.Amount,
                CurrencyCode = amount.Currency.Code,
                DecidedAt = decidedAt,
                ExpiresAtIfApproved = expiresAtIfApproved,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        try
        {
            var row = await connection.QuerySingleAsync<Row>(command);
            return new CardAuthorizationHoldReservationResult(row.Approved, row.DeclineReason);
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            // A true webhook redelivery raced past the earlier GetByStripeAuthorizationIdAsync
            // idempotency check and collided with itself on the unique index - re-read the
            // row this same StripeAuthorizationId already produced instead of surfacing a 500.
            var existing = await connection.QuerySingleAsync<ExistingRow>(new CommandDefinition(
                "SELECT Approved, DeclineReason FROM dbo.CardAuthorizations WHERE StripeAuthorizationId = @StripeAuthorizationId",
                new { StripeAuthorizationId = stripeAuthorizationId },
                cancellationToken: cancellationToken));
            return new CardAuthorizationHoldReservationResult(existing.Approved, existing.DeclineReason);
        }
    }

    private sealed record Row(bool Approved, string? DeclineReason);
    private sealed record ExistingRow(bool Approved, string? DeclineReason);
}
