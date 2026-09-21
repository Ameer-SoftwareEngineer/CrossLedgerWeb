using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Shared.Otc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

/// <summary>The OTC desk (specification 9's Ibanera-style dashboard): a large-trade
/// request gets a "negotiated" rate offer (CreateOtcRequestCommandHandler - dev-simulated,
/// no real trader), which the customer then accepts or rejects within a longer window
/// than a retail quote. Accepting runs the same Quote -> transfer path Send Money uses.</summary>
[ApiController]
[Route("api/v1/otc/requests")]
[Authorize(Roles = Roles.Customer)]
public sealed class OtcController : ControllerBase
{
    private readonly IMediator _mediator;

    public OtcController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType<OtcRequestResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OtcRequestResponse>> Create(CreateOtcRequestRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new CreateOtcRequestCommand(userId, new WalletId(request.SourceWalletId), request.FromCurrency, request.ToCurrency, request.Amount),
            cancellationToken);

        var response = new OtcRequestResponse(
            result.OtcRequestId.Value, result.QuoteId.Value, result.SourceWalletId.Value,
            result.FromCurrency.Code, result.ToCurrency.Code, result.RequestedAmount, result.DeskRate,
            result.RequestedAt, result.OfferExpiresAt, "Offered", null);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Runs through the same step-up gate as a regular transfer, since a
    /// successful accept posts one - RequireStepUp reads Amount off the request body,
    /// which the client echoes back from the offer it just received from Create.</summary>
    [HttpPost("{otcRequestId:guid}/accept")]
    [RequireStepUp(Operation = StepUpOperation.HighValueTransfer, ThresholdSetting = "Limits:StepUpAbove")]
    [ProducesResponseType<AcceptOtcRequestResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AcceptOtcRequestResponse>> Accept(
        Guid otcRequestId,
        AcceptOtcRequestRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new AcceptOtcRequestCommand(new OtcRequestId(otcRequestId), userId, new WalletId(request.TargetWalletId), idempotencyKey),
            cancellationToken);

        var response = new AcceptOtcRequestResponse(
            result.TransferId.Value, result.SourceAmount.Amount, result.SourceAmount.Currency.Code,
            result.TargetAmount.Amount, result.TargetAmount.Currency.Code, result.PostedAt);

        return Ok(response);
    }

    [HttpPost("{otcRequestId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid otcRequestId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _mediator.Send(new RejectOtcRequestCommand(new OtcRequestId(otcRequestId), userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Backs the OTC Desk history list - every request the caller has made,
    /// newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OtcRequestResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OtcRequestResponse>>> ListMine(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var requests = await _mediator.Send(new ListMyOtcRequestsQuery(userId), cancellationToken);

        var response = requests
            .Select(r => new OtcRequestResponse(
                r.OtcRequestId.Value, r.QuoteId.Value, r.SourceWalletId.Value, r.FromCurrency.Code, r.ToCurrency.Code,
                r.RequestedAmount, r.DeskRate, r.RequestedAt, r.OfferExpiresAt,
                r.IsExpired ? "Expired" : r.Status.ToString(), r.TransferId))
            .ToList();

        return Ok(response);
    }
}
