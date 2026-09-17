using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Wallets;

public sealed record TransactionHistoryEntry(
    LedgerEntryId Id, TransferId TransferId, LedgerDirection Direction, Money Amount, Money SignedAmount, DateTimeOffset PostedAt);

public sealed record TransactionHistoryPage(IReadOnlyList<TransactionHistoryEntry> Entries, int TotalCount, int PageNumber, int PageSize);
