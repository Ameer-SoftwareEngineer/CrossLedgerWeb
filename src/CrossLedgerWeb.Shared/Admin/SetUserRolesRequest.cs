namespace CrossLedgerWeb.Shared.Admin;

public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);
