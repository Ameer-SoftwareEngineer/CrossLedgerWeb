namespace CrossLedgerWeb.Application.Admin;

public sealed record SecurityAuditEventSummary(
    Guid? UserId,
    string Subject,
    string EventType,
    bool Succeeded,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset OccurredAt);

public sealed record SecurityAuditEventPage(IReadOnlyList<SecurityAuditEventSummary> Entries, int TotalCount, int PageNumber, int PageSize);
