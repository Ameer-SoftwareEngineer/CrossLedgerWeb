namespace CrossLedgerWeb.Providers.Stripe;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Signs the Issuing real-time authorization webhook (specification 12.3) -
    /// a separate secret from WebhookSecret above, which only covers the funding-rail
    /// payout webhook PaymentWebhooksController handles. Stripe issues a distinct signing
    /// secret per configured endpoint.</summary>
    public string IssuingWebhookSecret { get; set; } = string.Empty;
}
