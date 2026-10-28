using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by dbo.usp_GetWalletBalance (versioned in the separate
/// CrossLedgerDatabase project) - derives a wallet's balance with a raw SQL aggregation
/// instead of the Wallet aggregate, for admin tooling that wants a cross-check
/// independent of EF Core.</summary>
public interface IWalletBalanceReader
{
    Task<WalletBalance?> GetBalanceAsync(WalletId walletId, CancellationToken cancellationToken);
}

public sealed record WalletBalance(WalletId WalletId, Currency Currency, decimal Balance);
