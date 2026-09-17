using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Admin;

public sealed record UserSummary(UserId Id, string Email, IReadOnlyList<string> Roles, bool IsLockedOut);
