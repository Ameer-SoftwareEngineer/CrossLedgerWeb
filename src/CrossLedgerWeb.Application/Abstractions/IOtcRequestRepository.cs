using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface IOtcRequestRepository
{
    Task<OtcRequest?> GetByIdAsync(OtcRequestId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OtcRequest>> ListByRequesterAsync(UserId requestedByUserId, CancellationToken cancellationToken);

    /// <summary>Stages a new request for insertion - committed by UnitOfWorkBehavior's
    /// SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(OtcRequest otcRequest);
}
