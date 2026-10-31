namespace CrossLedgerWeb.Infrastructure.Webhooks;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    /// <summary>Empty by default, same as every provider's SecretKey - IWebhookNotifier
    /// treats an empty URL as "no subscriber configured yet" and no-ops rather than
    /// throwing, since unlike a routing provider this one is genuinely optional.</summary>
    public string SubscriberUrl { get; set; } = string.Empty;
    public string SubscriberSecret { get; set; } = string.Empty;
}
