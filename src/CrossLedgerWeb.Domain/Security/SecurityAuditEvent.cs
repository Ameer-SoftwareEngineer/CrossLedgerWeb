using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Security;

/// <summary>Specification 6.4: "every challenge, success, failure, device trust grant
/// and revocation is recorded with IP, user agent and timestamp." Append-only - there is
/// deliberately no method to edit or delete a row once written, and it is persisted
/// immediately by ISecurityAuditLogRepository rather than batched with
/// UnitOfWorkBehavior's SaveChanges, so a record of a failed attempt survives even
/// though the handler that wrote it goes on to throw.</summary>
public sealed class SecurityAuditEvent
{
    public SecurityAuditEventId Id { get; }
    public UserId? UserId { get; }
    public string Subject { get; }
    public SecurityEventType EventType { get; }
    public bool Succeeded { get; }
    public string? FailureReason { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public DateTimeOffset OccurredAt { get; }

    public SecurityAuditEvent(
        SecurityAuditEventId id,
        UserId? userId,
        string subject,
        SecurityEventType eventType,
        bool succeeded,
        string? failureReason,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("A subject is required.", nameof(subject));

        Id = id;
        UserId = userId;
        Subject = subject;
        EventType = eventType;
        Succeeded = succeeded;
        FailureReason = failureReason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredAt = occurredAt;
    }
}
