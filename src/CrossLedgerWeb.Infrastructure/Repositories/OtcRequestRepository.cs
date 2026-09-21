using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class OtcRequestRepository : IOtcRequestRepository
{
    private readonly CrossLedgerWebDbContext _db;

    public OtcRequestRepository(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public Task<OtcRequest?> GetByIdAsync(OtcRequestId id, CancellationToken cancellationToken) =>
        _db.OtcRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OtcRequest>> ListByRequesterAsync(UserId requestedByUserId, CancellationToken cancellationToken) =>
        await _db.OtcRequests.Where(r => r.RequestedByUserId == requestedByUserId).ToListAsync(cancellationToken);

    public void Add(OtcRequest otcRequest) => _db.OtcRequests.Add(otcRequest);
}
