using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListSecurityAuditEventsQuery(int PageNumber, int PageSize) : IRequest<SecurityAuditEventPage>;
