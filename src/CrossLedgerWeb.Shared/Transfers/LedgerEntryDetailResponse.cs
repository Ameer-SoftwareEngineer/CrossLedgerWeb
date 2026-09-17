namespace CrossLedgerWeb.Shared.Transfers;

public sealed record LedgerEntryDetailResponse(
    Guid Id, Guid TransferId, Guid WalletId, string Direction, decimal Amount, string Currency, decimal SignedAmount, DateTimeOffset PostedAt);
