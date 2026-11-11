using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Cards;

/// <summary>
/// A dev-simulated prepaid card (specification 9's Ibanera-style dashboard) - no real
/// card network or issuing processor sits behind this. Unlike a regular wallet, a card
/// never draws against the owner's other money directly: <see cref="CardWalletId"/>
/// points at a dedicated Wallet (WalletKind.Card) this card owns exclusively, which
/// starts at zero and is only ever funded by an explicit TopUp - the same reasoning
/// real prepaid cards work on. <see cref="Pan"/> and <see cref="Cvv"/> are plaintext on
/// this object; they are encrypted at rest by CardConfiguration's EF conversion, the same
/// way TwoFactorCredential.Secret is - reading them back out through the domain object is
/// exactly how "reveal" is supposed to work, which is why the API layer gates the reveal
/// endpoint with step-up rather than the entity hiding its own fields. Starts
/// PendingApproval, not Active - an admin review gate on issuance, same shape as
/// ApplicationUser.RegistrationStatus gating login.
/// </summary>
public sealed class Card
{
    public CardId Id { get; }
    public UserId OwnerId { get; }
    public WalletId CardWalletId { get; }
    public Currency Currency { get; }
    public CardType CardType { get; }
    public string CardholderName { get; }
    public string Pan { get; }
    public string Cvv { get; }
    public int ExpiryMonth { get; }
    public int ExpiryYear { get; }
    public DateTimeOffset IssuedAt { get; }
    public CardStatus Status { get; private set; }
    public decimal? SpendingLimit { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>Raw merchant category codes (e.g. "7995" for gambling) this card refuses
    /// to authorize against - empty means no category restriction. Checked by
    /// DecideCardAuthorizationCommandHandler against the webhook's own merchant_data, not
    /// by this entity (it has no notion of "the current authorization attempt").</summary>
    public IReadOnlySet<string> BlockedMerchantCategoryCodes { get; private set; } = new HashSet<string>();

    /// <summary>ISO-2 country codes this card may be used in - empty means unrestricted,
    /// same "empty/null = no restriction" convention SpendingLimit already uses.</summary>
    public IReadOnlySet<string> AllowedCountries { get; private set; } = new HashSet<string>();

    /// <summary>Rolling 24h / 30-day spend caps (from the moment of the authorization
    /// attempt, not calendar day/month boundaries - the same rolling-window reasoning
    /// AuthorizationHoldDays already uses, avoiding timezone/month-length edge cases).
    /// Measured against captured CardPurchase history, not open holds - see
    /// DecideCardAuthorizationCommandHandler.</summary>
    public decimal? DailySpendingLimit { get; private set; }
    public decimal? MonthlySpendingLimit { get; private set; }

    // Captured once at issuance, same spirit as a real card issuer's KYC intake -
    // display/record-keeping only, nothing else on this entity reads these.
    public SourceOfFunds SourceOfFunds { get; }
    public decimal EstimatedGrossAnnualIncome { get; }
    public decimal EstimatedNetWorth { get; }
    public bool IsActivelyEmployed { get; }
    public string? EmployerCompanyName { get; }
    public string? JobTitle { get; }
    public CardAddress RegisteredAddress { get; }
    public CardAddress ShippingAddress { get; }

    /// <summary>Null when ICardIssuer issued this card via SimulatedCardIssuer - only set
    /// when StripeCardIssuer actually created a real Stripe Issuing Cardholder/Card.</summary>
    public string? StripeCardholderId { get; }

    /// <summary>See StripeCardholderId - the two are always both null or both set.</summary>
    public string? StripeCardId { get; }

    /// <summary>Safe to show without step-up - derived from Pan rather than stored
    /// separately, so there is only ever one place a card's number can drift from its
    /// own last four digits.</summary>
    public string Last4 => Pan.Length >= 4 ? Pan[^4..] : Pan;

    public Card(
        CardId id,
        UserId ownerId,
        WalletId cardWalletId,
        Currency currency,
        CardType cardType,
        string cardholderName,
        string pan,
        string cvv,
        int expiryMonth,
        int expiryYear,
        DateTimeOffset issuedAt,
        SourceOfFunds sourceOfFunds,
        decimal estimatedGrossAnnualIncome,
        decimal estimatedNetWorth,
        bool isActivelyEmployed,
        string? employerCompanyName,
        string? jobTitle,
        CardAddress registeredAddress,
        CardAddress shippingAddress,
        string? stripeCardholderId = null,
        string? stripeCardId = null)
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
        CardWalletId = cardWalletId;
        Currency = currency;
        CardType = cardType;
        CardholderName = cardholderName;
        Pan = pan;
        Cvv = cvv;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        IssuedAt = issuedAt;
        SourceOfFunds = sourceOfFunds;
        EstimatedGrossAnnualIncome = estimatedGrossAnnualIncome;
        EstimatedNetWorth = estimatedNetWorth;
        IsActivelyEmployed = isActivelyEmployed;
        EmployerCompanyName = employerCompanyName;
        JobTitle = jobTitle;
        RegisteredAddress = registeredAddress;
        ShippingAddress = shippingAddress;
        StripeCardholderId = stripeCardholderId;
        StripeCardId = stripeCardId;
        Status = CardStatus.PendingApproval;
    }

    /// <summary>Rehydrates a card exactly as it was persisted, bypassing the public
    /// constructor's validation. Used by EF Core materialization only - Pan/Cvv arrive
    /// here already decrypted by CardConfiguration's value converter. RegisteredAddress
    /// and ShippingAddress are complex properties, which EF Core cannot bind through a
    /// constructor parameter (same reason LedgerEntry's rehydration constructor omits
    /// Money amount), so this constructor omits them too and EF sets them directly
    /// afterward.</summary>
    private Card(
        CardId id,
        UserId ownerId,
        WalletId cardWalletId,
        Currency currency,
        CardType cardType,
        string cardholderName,
        string pan,
        string cvv,
        int expiryMonth,
        int expiryYear,
        DateTimeOffset issuedAt,
        CardStatus status,
        decimal? spendingLimit,
        DateTimeOffset? closedAt,
        IReadOnlySet<string> blockedMerchantCategoryCodes,
        IReadOnlySet<string> allowedCountries,
        decimal? dailySpendingLimit,
        decimal? monthlySpendingLimit,
        SourceOfFunds sourceOfFunds,
        decimal estimatedGrossAnnualIncome,
        decimal estimatedNetWorth,
        bool isActivelyEmployed,
        string? employerCompanyName,
        string? jobTitle,
        string? stripeCardholderId,
        string? stripeCardId)
    {
        Id = id;
        OwnerId = ownerId;
        CardWalletId = cardWalletId;
        Currency = currency;
        CardType = cardType;
        CardholderName = cardholderName;
        Pan = pan;
        Cvv = cvv;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        IssuedAt = issuedAt;
        Status = status;
        SpendingLimit = spendingLimit;
        ClosedAt = closedAt;
        BlockedMerchantCategoryCodes = blockedMerchantCategoryCodes;
        AllowedCountries = allowedCountries;
        DailySpendingLimit = dailySpendingLimit;
        MonthlySpendingLimit = monthlySpendingLimit;
        SourceOfFunds = sourceOfFunds;
        EstimatedGrossAnnualIncome = estimatedGrossAnnualIncome;
        EstimatedNetWorth = estimatedNetWorth;
        IsActivelyEmployed = isActivelyEmployed;
        EmployerCompanyName = employerCompanyName;
        JobTitle = jobTitle;
        StripeCardholderId = stripeCardholderId;
        StripeCardId = stripeCardId;
        RegisteredAddress = default!;
        ShippingAddress = default!;
    }

    /// <summary>An admin cleared the issuance request (specification 9's Admin Console) -
    /// the card can now be used. Mirrors ApplicationUser's own RegistrationStatus gate on
    /// login: credentials/intake alone were never enough.</summary>
    public void Approve()
    {
        if (Status != CardStatus.PendingApproval)
            throw new InvalidCardStateException(Id, Status, "approve");

        Status = CardStatus.Active;
    }

    /// <summary>An admin declined the request - terminal, same as Close, but reachable
    /// only from PendingApproval so an already-Active card can't be retroactively
    /// rejected.</summary>
    public void Reject()
    {
        if (Status != CardStatus.PendingApproval)
            throw new InvalidCardStateException(Id, Status, "reject");

        Status = CardStatus.Rejected;
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

    /// <summary>One combined setter (one step-up-gated save action) for the MCC block
    /// list, country allow-list, and the two rolling-window spend caps - kept together
    /// because the frontend presents them as a single "spending controls" editor, the same
    /// way SetSpendingLimit is its own separate action for the per-purchase cap.</summary>
    public void SetSpendingControls(
        IReadOnlySet<string> blockedMerchantCategoryCodes,
        IReadOnlySet<string> allowedCountries,
        decimal? dailyLimit,
        decimal? monthlyLimit)
    {
        if (dailyLimit is < 0)
            throw new ArgumentOutOfRangeException(nameof(dailyLimit), dailyLimit, "The daily spending limit cannot be negative.");

        if (monthlyLimit is < 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyLimit), monthlyLimit, "The monthly spending limit cannot be negative.");

        BlockedMerchantCategoryCodes = blockedMerchantCategoryCodes;
        AllowedCountries = allowedCountries;
        DailySpendingLimit = dailyLimit;
        MonthlySpendingLimit = monthlyLimit;
    }

    /// <summary>Throws if this card cannot be used for a purchase of this amount right
    /// now - not Active, or over the per-purchase spending limit. Whether the card's own
    /// wallet actually holds enough funds is Wallet.Debit's concern, checked when the
    /// purchase is posted, not this entity's.</summary>
    public void EnsureCanSpend(Money amount)
    {
        if (Status != CardStatus.Active)
            throw new InvalidCardStateException(Id, Status, "spend on");

        if (SpendingLimit is { } limit && amount.Amount > limit)
            throw new CardSpendingLimitExceededException(Id, amount.Amount, limit);
    }
}
