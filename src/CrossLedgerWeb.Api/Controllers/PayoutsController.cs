using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Shared.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

[ApiController]
[Route("api/v1/payouts")]
[Authorize(Roles = Roles.Customer)]
public sealed class PayoutsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayoutsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Every mutating endpoint requires an Idempotency-Key header
    /// (specification 2.3) - a repeated key replays the stored response instead of
    /// reserving and submitting the payout again.</summary>
    [HttpPost]
    [ProducesResponseType<PayoutResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PayoutResponse>> Create(
        CreatePayoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RoutingPreference>(request.Preference, ignoreCase: true, out var preference))
        {
            return BadRequest(new { code = "INVALID_PREFERENCE", message = $"Unknown routing preference '{request.Preference}'." });
        }

        var command = new ExecutePayoutCommand(
            new WalletId(request.SourceWalletId),
            request.TargetCurrency,
            request.DestinationCountry,
            request.Amount,
            preference,
            idempotencyKey);

        var result = await _mediator.Send(command, cancellationToken);

        var response = new PayoutResponse(
            result.PayoutId.Value,
            result.TransferId.Value,
            result.State.ToString(),
            result.ProviderCode?.ToString(),
            result.ProviderReference);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Backs the Routing Inspector (specification 9) - every provider the
    /// routing engine scored for this payout, ranked, not just the one it picked
    /// (specification 5.3's audit requirement). 404s identically whether the payout
    /// doesn't exist or the caller doesn't own its source wallet.</summary>
    [HttpGet("{payoutId:guid}/routing-decision")]
    [ProducesResponseType<IReadOnlyList<RoutingDecisionEntryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoutingDecisionEntryResponse>>> GetRoutingDecision(Guid payoutId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var entries = await _mediator.Send(new GetRoutingDecisionQuery(new PayoutId(payoutId), userId), cancellationToken);

        var response = entries
            .Select(e => new RoutingDecisionEntryResponse(
                e.ProviderCode, e.Rank, e.Score, e.Fee.Amount, e.Fee.Currency.Code, e.EstimatedSettlementMinutes, e.RecordedAt))
            .ToList();

        return Ok(response);
    }
}
