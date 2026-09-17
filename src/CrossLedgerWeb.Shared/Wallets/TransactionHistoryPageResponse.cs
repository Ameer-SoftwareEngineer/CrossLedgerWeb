namespace CrossLedgerWeb.Shared.Wallets;

public sealed record TransactionHistoryEntryResponse(
    Guid Id, Guid TransferId, string Direction, decimal Amount, string Currency, decimal SignedAmount, DateTimeOffset PostedAt);

public sealed record TransactionHistoryPageResponse(
    IReadOnlyList<TransactionHistoryEntryResponse> Entries, int TotalCount, int PageNumber, int PageSize);
