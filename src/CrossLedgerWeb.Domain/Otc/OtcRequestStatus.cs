namespace CrossLedgerWeb.Domain.Otc;

/// <summary>Expiry is never a fourth persisted state here, same as <see cref="Fx.Quote"/> -
/// an Offered request past its OfferExpiresAt is checked lazily (IsExpired), and accepting
/// one lets the underlying Quote's own expiry check reject it.</summary>
public enum OtcRequestStatus
{
    Offered,
    Accepted,
    Rejected,
}
