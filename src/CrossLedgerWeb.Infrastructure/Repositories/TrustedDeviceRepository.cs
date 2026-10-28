using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class TrustedDeviceRepository : ITrustedDeviceRepository
{
    private readonly CrossLedgerWebDbContext _db;

    public TrustedDeviceRepository(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public Task<TrustedDevice?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _db.TrustedDevices.FirstOrDefaultAsync(d => d.TokenHash == tokenHash, cancellationToken);

    public Task<TrustedDevice?> GetByIdAsync(TrustedDeviceId id, CancellationToken cancellationToken) =>
        _db.TrustedDevices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TrustedDevice>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken) =>
        await _db.TrustedDevices.Where(d => d.UserId == userId).ToListAsync(cancellationToken);

    public void Add(TrustedDevice device) => _db.TrustedDevices.Add(device);
}
