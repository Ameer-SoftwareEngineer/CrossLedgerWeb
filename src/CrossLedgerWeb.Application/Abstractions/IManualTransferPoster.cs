using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by dbo.usp_PostTransfer (versioned in the separate
/// CrossLedgerDatabase project) - posts the same four balanced ledger entries
/// TransferPoster.Post does in-process, but atomically in one round trip under
/// UPDLOCK/HOLDLOCK. Used for the admin "manual transfer" tool, which moves money
/// directly between two wallets without a Quote - the regular customer-facing transfer
/// flow (CreateTransferCommandHandler) keeps using the EF-based TransferPoster
/// unchanged.</summary>
public interface IManualTransferPoster
{
    Task<ManualTransferResult> PostAsync(
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
        CancellationToken cancellationToken);
}

public sealed record ManualTransferResult(TransferId TransferId, string Status, DateTimeOffset PostedAt);
