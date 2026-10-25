using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class SecurityAuditLogRepository : ISecurityAuditLogRepository
{
    private readonly CrossLedgerWebDbContext _db;

    public SecurityAuditLogRepository(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    // Deliberately saves immediately rather than staging for UnitOfWorkBehavior - a
    // failed login or step-up attempt must be recorded even though the handler that
    // calls this goes on to throw and abort the rest of the pipeline, which would
    // otherwise discard everything staged on this DbContext, audit row included.
    public async Task RecordAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _db.SecurityAuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SecurityAuditEventPage> ListRecentAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.SecurityAuditEvents.OrderByDescending(e => e.OccurredAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new SecurityAuditEventSummary(
                e.UserId.HasValue ? e.UserId.Value.Value : (Guid?)null,
                e.Subject,
                e.EventType.ToString(),
                e.Succeeded,
                e.FailureReason,
                e.IpAddress,
                e.UserAgent,
                e.OccurredAt))
            .ToListAsync(cancellationToken);

        return new SecurityAuditEventPage(page, totalCount, pageNumber, pageSize);
    }
}
