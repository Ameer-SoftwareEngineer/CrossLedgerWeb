using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Ledger;
using CrossLedgerWeb.Application.Transfers;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Shared.Transfers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

[ApiController]
[Route("api/v1/transfers")]
public sealed class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransfersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Every mutating endpoint requires an Idempotency-Key header
    /// (specification 2.3) - a repeated key replays the stored response instead of
    /// posting the transfer again. RequireStepUp only actually challenges the caller
    /// once SourceAmount clears Limits:StepUpAbove (specification 6.2's worked example,
    /// 6.3) - everyday transfers below the threshold pass straight through.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [RequireStepUp(Operation = StepUpOperation.HighValueTransfer, ThresholdSetting = "Limits:StepUpAbove")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TransferResponse>> Create(
        CreateTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransferCommand(
            new QuoteId(request.QuoteId),
            new WalletId(request.SourceWalletId),
            new WalletId(request.TargetWalletId),
            request.SourceAmount,
            idempotencyKey);

        var result = await _mediator.Send(command, cancellationToken);

        var response = new TransferResponse(
            result.TransferId.Value,
            result.SourceAmount.Amount,
            result.SourceAmount.Currency.Code,
            result.TargetAmount.Amount,
            result.TargetAmount.Currency.Code,
            result.PostedAt);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Backs the Ledger Viewer (specification 9) - every entry a transfer
    /// posted, across all the wallets it touched, so the double-entry breakdown is
    /// visible in one call. 404s identically whether the transfer doesn't exist or the
    /// caller wasn't a party to it (see GetTransferLedgerEntriesQueryHandler).</summary>
    [HttpGet("{transferId:guid}/ledger-entries")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType<IReadOnlyList<LedgerEntryDetailResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LedgerEntryDetailResponse>>> GetLedgerEntries(Guid transferId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var entries = await _mediator.Send(new GetTransferLedgerEntriesQuery(new TransferId(transferId), userId), cancellationToken);

        var response = entries
            .Select(e => new LedgerEntryDetailResponse(
                e.Id.Value, e.TransferId.Value, e.WalletId.Value, e.Direction.ToString(), e.Amount.Amount, e.Amount.Currency.Code, e.SignedAmount.Amount, e.PostedAt))
            .ToList();

        return Ok(response);
    }
}
