using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;

namespace CrossLedgerWeb.Application.Abstractions;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(WalletId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Wallet>> ListByOwnerAsync(UserId ownerId, CancellationToken cancellationToken);

    /// <summary>A projection that never touches LedgerEntries - for callers (like the
    /// transaction history endpoint) that only need to check who owns a wallet, not
    /// derive its balance.</summary>
    Task<UserId?> GetOwnerIdAsync(WalletId id, CancellationToken cancellationToken);

    /// <summary>Stages a new wallet for insertion - committed by UnitOfWorkBehavior's
    /// SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(Wallet wallet);
}
