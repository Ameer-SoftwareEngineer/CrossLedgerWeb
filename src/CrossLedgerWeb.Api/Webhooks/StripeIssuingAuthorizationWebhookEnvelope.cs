using System.Text.Json.Serialization;

namespace CrossLedgerWeb.Api.Webhooks;

/// <summary>
/// The exact JSON shape of a Stripe issuing_authorization.request webhook event (specification
/// 12.3), fetched directly from Stripe's own, current, publicly documented
/// docs.stripe.com/api/issuing/authorizations/object - not guessed. Deliberately narrow:
/// only the fields DecideCardAuthorizationCommand actually needs, not the full Authorization
/// object. The decidable amount lives under pending_request, not the top-level
/// amount/currency - those are 0/the cardholder's billing currency while a request is still
/// pending, per Stripe's own docs.
/// </summary>
public sealed record StripeIssuingWebhookEnvelope(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("data")] StripeIssuingWebhookData? Data);

public sealed record StripeIssuingWebhookData(
    [property: JsonPropertyName("object")] StripeIssuingAuthorization? Object);

public sealed record StripeIssuingAuthorization(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("card")] StripeIssuingCardRef Card,
    [property: JsonPropertyName("pending_request")] StripeIssuingPendingRequest? PendingRequest,
    [property: JsonPropertyName("merchant_data")] StripeIssuingMerchantData? MerchantData);

public sealed record StripeIssuingCardRef([property: JsonPropertyName("id")] string Id);

public sealed record StripeIssuingPendingRequest(
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("currency")] string Currency);

public sealed record StripeIssuingMerchantData([property: JsonPropertyName("name")] string? Name);
