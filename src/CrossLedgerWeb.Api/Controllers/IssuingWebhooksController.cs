using System.Text;
using System.Text.Json;
using CrossLedgerWeb.Api.Webhooks;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Providers.Common;
using CrossLedgerWeb.Providers.Stripe;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CrossLedgerWeb.Api.Controllers;

/// <summary>
/// Receives Stripe Issuing's real-time authorization webhook (specification 12.3) - the
/// synchronous issuing_authorization.request event Stripe sends when a Stripe-issued card
/// is presented, needing an approve/decline decision back within a ~2 second budget, in the
/// HTTP response body itself rather than a follow-up API call. Contract verified directly
/// against docs.stripe.com/issuing/controls/real-time-authorizations: 200 status, JSON body
/// {"approved": bool}, and a Stripe-Version response header or Stripe treats the response as
/// a webhook_error and falls back to its own Autopilot/timeout handling - set here from the
/// installed Stripe.net SDK's own StripeConfiguration.ApiVersion constant so it can never
/// drift out of sync with what this code was actually written against.
///
/// [AllowAnonymous] for the same reason PaymentWebhooksController is - Stripe cannot present
/// one of our bearer tokens, so the Stripe-Signature check is what stands in for
/// authentication.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/issuing")]
[AllowAnonymous]
public sealed class IssuingWebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOptions<StripeOptions> _stripeOptions;

    public IssuingWebhooksController(IMediator mediator, IOptions<StripeOptions> stripeOptions)
    {
        _mediator = mediator;
        _stripeOptions = stripeOptions;
    }

    [HttpPost("authorization")]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }
        Request.Body.Position = 0;

        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrEmpty(signature)
            || !StripeSignatureVerifier.Verify(rawBody, signature, _stripeOptions.Value.IssuingWebhookSecret))
        {
            return Unauthorized();
        }

        StripeIssuingWebhookEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<StripeIssuingWebhookEnvelope>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest(new { code = "INVALID_PAYLOAD", message = "Webhook body is not valid JSON." });
        }

        var authorization = envelope?.Data?.Object;
        var pendingRequest = authorization?.PendingRequest;

        // Not our event type, or a pending_request-less authorization (already decided,
        // a redelivery of a non-request event, etc.) - decline safely rather than guess.
        if (envelope?.Type != "issuing_authorization.request" || authorization is null || pendingRequest is null)
        {
            SetStripeVersionHeader();
            return Ok(new { approved = false });
        }

        var result = await _mediator.Send(
            new DecideCardAuthorizationCommand(
                authorization.Id,
                authorization.Card.Id,
                pendingRequest.Amount,
                pendingRequest.Currency,
                authorization.MerchantData?.Name ?? "Unknown merchant"),
            cancellationToken);

        SetStripeVersionHeader();

        if (!result.Approved)
            return Ok(new { approved = false, metadata = new Dictionary<string, string> { ["decline_reason"] = result.DeclineReason ?? "unknown" } });

        return Ok(new { approved = true });
    }

    private void SetStripeVersionHeader() =>
        Response.Headers["Stripe-Version"] = Stripe.StripeConfiguration.ApiVersion;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
