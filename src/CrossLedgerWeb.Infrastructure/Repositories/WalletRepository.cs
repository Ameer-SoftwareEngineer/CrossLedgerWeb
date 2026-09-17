using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly CrossLedgerWebDbContext _db;

    public WalletRepository(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public Task<Wallet?> GetByIdAsync(WalletId id, CancellationToken cancellationToken) =>
        _db.Wallets.Include(w => w.Entries).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Wallet>> ListByOwnerAsync(UserId ownerId, CancellationToken cancellationToken) =>
        await _db.Wallets.Include(w => w.Entries).Where(w => w.OwnerId == ownerId).ToListAsync(cancellationToken);

    public async Task<UserId?> GetOwnerIdAsync(WalletId id, CancellationToken cancellationToken)
    {
        var match = await _db.Wallets.AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new { w.OwnerId })
            .FirstOrDefaultAsync(cancellationToken);

        return match?.OwnerId;
    }

    public void Add(Wallet wallet) => _db.Wallets.Add(wallet);
}
