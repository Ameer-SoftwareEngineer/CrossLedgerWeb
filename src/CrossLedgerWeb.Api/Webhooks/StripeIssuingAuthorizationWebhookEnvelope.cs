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

/// <summary>category_code is the raw MCC (e.g. "5734"); country is ISO-2 - both confirmed
/// directly from Stripe's own Authorization object docs (not guessed), used by the
/// spending-controls checks in DecideCardAuthorizationCommandHandler.</summary>
public sealed record StripeIssuingMerchantData(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("category_code")] string? CategoryCode,
    [property: JsonPropertyName("country")] string? Country);

/// <summary>The Transaction object a issuing_transaction.created event carries (specification
/// 12.3/12.4's "Settlement" week), fetched directly from Stripe's own, current, publicly
/// documented docs.stripe.com/issuing/purchases/transactions - a capture, not an
/// authorization, delivered to Stripe's ordinary webhook endpoint (never the synchronous
/// authorization one - see IssuingWebhooksController's own doc comment). Only the fields
/// CaptureCardAuthorizationCommand needs; Amount's sign reflects Stripe's own Issuing-balance
/// accounting convention (negative for a capture) and is not meaningful to this codebase -
/// only its magnitude is used.</summary>
public sealed record StripeIssuingTransaction(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("authorization")] string? Authorization);

public sealed record StripeIssuingTransactionWebhookData(
    [property: JsonPropertyName("object")] StripeIssuingTransaction? Object);

public sealed record StripeIssuingTransactionWebhookEnvelope(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("data")] StripeIssuingTransactionWebhookData? Data);
