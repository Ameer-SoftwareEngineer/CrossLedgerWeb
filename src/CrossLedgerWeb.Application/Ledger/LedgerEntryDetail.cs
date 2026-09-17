using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Ledger;

/// <summary>One posting from a transfer's full double-entry breakdown (specification 9's
/// Ledger Viewer). Unlike TransactionHistoryEntry, this carries WalletId - the whole
/// point here is showing which of the transfer's four wallets each entry belongs to.</summary>
public sealed record LedgerEntryDetail(
    LedgerEntryId Id, TransferId TransferId, WalletId WalletId, LedgerDirection Direction, Money Amount, Money SignedAmount, DateTimeOffset PostedAt);
