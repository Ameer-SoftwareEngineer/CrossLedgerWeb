namespace CrossLedgerWeb.Infrastructure.Cards;

public sealed class CardIssuingOptions
{
    public const string SectionName = "CardIssuing";

    /// <summary>Off by default - every existing test, CI run and dev setup keeps using
    /// SimulatedCardIssuer unless this is explicitly turned on, since StripeCardIssuer
    /// needs a real Stripe:SecretKey and has never been run against a live sandbox.</summary>
    public bool UseStripe { get; set; }
}
