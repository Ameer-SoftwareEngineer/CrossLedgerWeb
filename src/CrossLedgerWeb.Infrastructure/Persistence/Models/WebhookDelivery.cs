namespace CrossLedgerWeb.Infrastructure.Persistence.Models;

/// <summary>Pure persistence record - not a domain concept, just the backing store for
/// IWebhookNotifier's delivery log (specification 2.6/9), same spirit as
/// ProcessedWebhookEvent on the inbound side.</summary>
public sealed class WebhookDelivery
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string SubscriberUrl { get; set; } = default!;
    public bool Succeeded { get; set; }
    public int? ResponseStatusCode { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
}
