using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Wallets;

/// <summary>
/// A wallet never stores its own balance. <see cref="Balance"/> is derived by summing
/// the signed amount of every posted <see cref="LedgerEntry"/>, so it can never drift
/// from the entries that justify it.
/// </summary>
public sealed class Wallet
{
    private readonly List<LedgerEntry> _entries = new();

    public WalletId Id { get; }
    public UserId OwnerId { get; }
    public Currency Currency { get; }
    public WalletKind Kind { get; }

    /// <summary>Exists so a debit/credit produces a real UPDATE against this wallet's own
    /// row, not just INSERTs into LedgerEntries - without that, SQL Server's rowversion
    /// column never actually changes on a Post, so EF Core's optimistic concurrency check
    /// (specification 2.4) never has anything to compare and silently never fires. Two
    /// concurrent debits would otherwise both read the same balance, both pass the
    /// sufficient-funds check, and both succeed - a genuine double-spend, found by
    /// running real concurrent load against this exact code path, not by inspection.</summary>
    public DateTimeOffset LastModifiedAt { get; private set; }

    public IReadOnlyList<LedgerEntry> Entries => _entries;

    public Money Balance => _entries.Aggregate(Money.Zero(Currency), (balance, entry) => balance + entry.SignedAmount);

    public Wallet(WalletId id, UserId ownerId, Currency currency, WalletKind kind = WalletKind.Customer)
    {
        Id = id;
        OwnerId = ownerId;
        Currency = currency;
        Kind = kind;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public LedgerEntry Debit(Money amount, TransferId transferId, DateTimeOffset postedAt) =>
        Post(LedgerDirection.Debit, amount, transferId, postedAt);

    public LedgerEntry Credit(Money amount, TransferId transferId, DateTimeOffset postedAt) =>
        Post(LedgerDirection.Credit, amount, transferId, postedAt);

    private LedgerEntry Post(LedgerDirection direction, Money amount, TransferId transferId, DateTimeOffset postedAt)
    {
        if (amount.Currency != Currency)
            throw new CurrencyMismatchException(Currency, amount.Currency);

        // Customer and Card wallets must never overdraw; SystemClearing, PayoutReserve
        // and MerchantSettlement are internal settlement accounts that legitimately run
        // negative between the two legs of whatever they're clearing.
        if (direction == LedgerDirection.Debit && Kind is WalletKind.Customer or WalletKind.Card && Balance < amount)
            throw new InsufficientFundsException(Id, Balance, amount);

        var entry = new LedgerEntry(LedgerEntryId.New(), transferId, Id, direction, amount, postedAt);
        _entries.Add(entry);
        LastModifiedAt = postedAt;
        return entry;
    }
}
