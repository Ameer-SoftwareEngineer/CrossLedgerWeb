namespace CrossLedgerWeb.Shared.Admin;

public sealed record SecurityAuditEventResponse(
    Guid? UserId,
    string Subject,
    string EventType,
    bool Succeeded,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset OccurredAt);

public sealed record SecurityAuditEventPageResponse(IReadOnlyList<SecurityAuditEventResponse> Entries, int TotalCount, int PageNumber, int PageSize);
