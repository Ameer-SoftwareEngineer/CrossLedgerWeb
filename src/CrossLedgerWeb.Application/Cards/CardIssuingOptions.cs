namespace CrossLedgerWeb.Application.Cards;

/// <summary>Lives in Application, not Infrastructure, even though SimulatedCardIssuer/
/// StripeCardIssuer are Infrastructure concerns (selected via DependencyInjection.cs's own
/// IOptions-driven factory) - DecideCardAuthorizationCommandHandler, an Application-layer
/// handler, needs AuthorizationHoldDays directly, and Application must never reference
/// Infrastructure (Clean Architecture's dependencies-point-inward rule, enforced everywhere
/// else in this codebase). Infrastructure still binds and configures this exact type via
/// services.Configure - the physical project a plain options class lives in doesn't change
/// who can read it.</summary>
public sealed class CardIssuingOptions
{
    public const string SectionName = "CardIssuing";

    /// <summary>Off by default - every existing test, CI run and dev setup keeps using
    /// SimulatedCardIssuer unless this is explicitly turned on, since StripeCardIssuer
    /// needs a real Stripe:SecretKey and has never been run against a live sandbox.</summary>
    public bool UseStripe { get; set; }

    /// <summary>How long an approved real-time authorization holds funds before the expiry
    /// sweeper releases it uncaptured (specification 12.3/12.4's "Settlement" week) - 7 days
    /// matches the common real-world card network default (Stripe's own docs note hotels,
    /// airlines and car rentals can run up to 31 days, but 7 is the ordinary case).</summary>
    public int AuthorizationHoldDays { get; set; } = 7;
}
