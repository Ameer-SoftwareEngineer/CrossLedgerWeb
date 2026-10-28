namespace CrossLedgerWeb.Shared.Wallets;

public sealed record AccountStatementEntryResponse(
    Guid Id, Guid TransferId, string Direction, decimal Amount, DateTimeOffset PostedAt, decimal RunningBalance);

public sealed record AccountStatementResponse(
    string Currency, decimal OpeningBalance, IReadOnlyList<AccountStatementEntryResponse> Entries);
