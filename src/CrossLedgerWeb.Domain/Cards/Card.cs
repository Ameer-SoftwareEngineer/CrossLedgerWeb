using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Cards;

/// <summary>
/// A dev-simulated virtual card linked to one of the owner's wallets - no real card
/// network or issuing processor sits behind this (specification 9's Ibanera-style
/// dashboard). <see cref="Pan"/> and <see cref="Cvv"/> are plaintext on this object; they
/// are encrypted at rest by CardConfiguration's EF conversion, the same way
/// TwoFactorCredential.Secret is - reading them back out through the domain object is
/// exactly how "reveal" is supposed to work, which is why the API layer gates the reveal
/// endpoint with step-up rather than the entity hiding its own fields.
/// </summary>
public sealed class Card
{
    public CardId Id { get; }
    public UserId OwnerId { get; }
    public WalletId LinkedWalletId { get; }
    public Currency Currency { get; }
    public string CardholderName { get; }
    public string Pan { get; }
    public string Cvv { get; }
    public int ExpiryMonth { get; }
    public int ExpiryYear { get; }
    public DateTimeOffset IssuedAt { get; }
    public CardStatus Status { get; private set; }
    public decimal? SpendingLimit { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>Safe to show without step-up - derived from Pan rather than stored
    /// separately, so there is only ever one place a card's number can drift from its
    /// own last four digits.</summary>
    public string Last4 => Pan.Length >= 4 ? Pan[^4..] : Pan;

    public Card(
        CardId id,
        UserId ownerId,
        WalletId linkedWalletId,
        Currency currency,
        string cardholderName,
        string pan,
        string cvv,
        int expiryMonth,
        int expiryYear,
        DateTimeOffset issuedAt)
    {
        if (string.IsNullOrWhiteSpace(cardholderName))
            throw new ArgumentException("A card requires a cardholder name.", nameof(cardholderName));

        if (string.IsNullOrWhiteSpace(pan) || pan.Length < 12 || !pan.All(char.IsDigit))
            throw new ArgumentException("The PAN must be a numeric string of at least 12 digits.", nameof(pan));

        if (cvv.Length != 3 || !cvv.All(char.IsDigit))
            throw new ArgumentException("The CVV must be exactly 3 digits.", nameof(cvv));

        if (expiryMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(expiryMonth), expiryMonth, "Expiry month must be between 1 and 12.");

        Id = id;
        OwnerId = ownerId;
        LinkedWalletId = linkedWalletId;
        Currency = currency;
        CardholderName = cardholderName;
        Pan = pan;
        Cvv = cvv;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        IssuedAt = issuedAt;
        Status = CardStatus.Active;
    }

    /// <summary>Rehydrates a card exactly as it was persisted, bypassing the public
    /// constructor's validation. Used by EF Core materialization only - Pan/Cvv arrive
    /// here already decrypted by CardConfiguration's value converter.</summary>
    private Card(
        CardId id,
        UserId ownerId,
        WalletId linkedWalletId,
        Currency currency,
        string cardholderName,
        string pan,
        string cvv,
        int expiryMonth,
        int expiryYear,
        DateTimeOffset issuedAt,
        CardStatus status,
        decimal? spendingLimit,
        DateTimeOffset? closedAt)
    {
        Id = id;
        OwnerId = ownerId;
        LinkedWalletId = linkedWalletId;
        Currency = currency;
        CardholderName = cardholderName;
        Pan = pan;
        Cvv = cvv;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        IssuedAt = issuedAt;
        Status = status;
        SpendingLimit = spendingLimit;
        ClosedAt = closedAt;
    }

    public void Freeze()
    {
        if (Status != CardStatus.Active)
            throw new InvalidCardStateException(Id, Status, "freeze");

        Status = CardStatus.Frozen;
    }

    public void Unfreeze()
    {
        if (Status != CardStatus.Frozen)
            throw new InvalidCardStateException(Id, Status, "unfreeze");

        Status = CardStatus.Active;
    }

    public void Close(DateTimeOffset asOf)
    {
        if (Status == CardStatus.Closed)
            throw new InvalidCardStateException(Id, Status, "close");

        Status = CardStatus.Closed;
        ClosedAt = asOf;
    }

    public void SetSpendingLimit(decimal? limit)
    {
        if (limit is < 0)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "The spending limit cannot be negative.");

        SpendingLimit = limit;
    }

    /// <summary>Throws if this card cannot be used for a purchase of this amount right
    /// now - not Active, or over the per-purchase spending limit. Checked before posting
    /// any ledger entries, same as Wallet.Debit checks sufficient funds before posting.</summary>
    public void EnsureCanSpend(Money amount)
    {
        if (Status != CardStatus.Active)
            throw new InvalidCardStateException(Id, Status, "spend on");

        if (SpendingLimit is { } limit && amount.Amount > limit)
            throw new CardSpendingLimitExceededException(Id, amount.Amount, limit);
    }
}
