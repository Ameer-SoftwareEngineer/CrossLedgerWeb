using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by dbo.usp_CheckTransferLimits (versioned in the separate
/// CrossLedgerDatabase project) - an AML rolling-24h-limit pre-check, scoped per
/// (owner, currency). It only reports; the caller decides whether to block.</summary>
public interface ITransferLimitsReader
{
    Task<TransferLimitCheck> CheckAsync(UserId ownerId, Currency currency, decimal proposedAmount, CancellationToken cancellationToken);
}

public sealed record TransferLimitCheck(decimal RollingTotal, decimal ProjectedTotal, decimal RollingLimit, bool ExceedsLimit);
