namespace CrossLedgerWeb.Domain.Cards;

/// <summary>Mirrors Stripe Issuing's own real shipping.status vocabulary
/// (pending -> shipped -> delivered, or returned/failure) - confirmed directly from
/// Stripe's testing docs, not guessed.</summary>
public enum CardFulfillmentStatus
{
    /// <summary>Virtual cards - nothing to ship, ever.</summary>
    NotApplicable,

    /// <summary>Physical, approved, not yet shipped.</summary>
    Pending,

    Shipped,
    Delivered,
    Returned,
    Failed,
}
