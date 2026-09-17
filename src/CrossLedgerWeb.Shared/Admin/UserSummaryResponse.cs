namespace CrossLedgerWeb.Shared.Admin;

public sealed record UserSummaryResponse(Guid Id, string Email, IReadOnlyList<string> Roles, bool IsLockedOut);
