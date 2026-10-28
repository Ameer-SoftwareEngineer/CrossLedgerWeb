using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public interface ITrustedDeviceRepository
{
    Task<TrustedDevice?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<TrustedDevice?> GetByIdAsync(TrustedDeviceId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TrustedDevice>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Stages a new grant for insertion - committed by UnitOfWorkBehavior's
    /// SaveChanges at the end of the pipeline, not immediately.</summary>
    void Add(TrustedDevice device);
}
