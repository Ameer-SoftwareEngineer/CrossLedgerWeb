namespace CrossLedgerWeb.Shared.Admin;

public sealed record ProviderHealthResponse(string ProviderCode, string Status, DateTimeOffset CheckedAt);
