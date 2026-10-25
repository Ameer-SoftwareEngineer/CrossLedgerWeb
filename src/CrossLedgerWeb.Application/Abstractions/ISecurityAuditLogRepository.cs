using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.Security;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Deliberately its own store rather than routing through IUnitOfWork's
/// batched SaveChanges: RecordAsync persists immediately, so an audit row for a failed
/// login or step-up attempt is durable even though the handler that wrote it goes on to
/// throw and abort the rest of the pipeline.</summary>
public interface ISecurityAuditLogRepository
{
    Task RecordAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken);

    /// <summary>Backs the Admin Console's security log (specification 9).</summary>
    Task<SecurityAuditEventPage> ListRecentAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
