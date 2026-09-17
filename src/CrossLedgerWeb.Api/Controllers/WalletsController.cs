using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Shared.Wallets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

[ApiController]
[Route("api/v1/wallets")]
[Authorize(Roles = Roles.Customer)]
public sealed class WalletsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType<CreateWalletResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateWalletResponse>> Create(CreateWalletRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateWalletCommand(new UserId(request.OwnerId), request.Currency), cancellationToken);

        var response = new CreateWalletResponse(result.WalletId.Value, result.OwnerId.Value, result.Currency.Code);
        return CreatedAtAction(nameof(GetBalance), new { walletId = response.WalletId }, response);
    }

    [HttpGet("{walletId:guid}/balance")]
    [ProducesResponseType<WalletBalanceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletBalanceResponse>> GetBalance(Guid walletId, CancellationToken cancellationToken)
    {
        var balance = await _mediator.Send(new GetWalletBalanceQuery(new WalletId(walletId)), cancellationToken);

        return Ok(new WalletBalanceResponse(walletId, balance.Amount, balance.Currency.Code));
    }

    /// <summary>Backs the Wallet Dashboard (specification 9) - every wallet the caller
    /// owns, with its derived balance, in one call.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WalletSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WalletSummaryResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var wallets = await _mediator.Send(new ListMyWalletsQuery(userId), cancellationToken);

        var response = wallets
            .Select(w => new WalletSummaryResponse(w.WalletId.Value, w.Currency.Code, w.Balance.Amount))
            .ToList();

        return Ok(response);
    }

    /// <summary>Backs the Transaction History screen (specification 9) - server-side
    /// paged over dbo.usp_GetTransactionHistory (specification 4.1). 404s for a wallet
    /// that exists but belongs to someone else, same as for one that doesn't exist at
    /// all, so this can never be used to probe another user's wallet ids.</summary>
    [HttpGet("{walletId:guid}/transactions")]
    [ProducesResponseType<TransactionHistoryPageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionHistoryPageResponse>> GetTransactions(
        Guid walletId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var page = await _mediator.Send(
            new GetTransactionHistoryQuery(
                new WalletId(walletId),
                userId,
                pageNumber <= 0 ? 1 : pageNumber,
                pageSize <= 0 ? 20 : pageSize,
                fromDate,
                toDate),
            cancellationToken);

        var response = new TransactionHistoryPageResponse(
            page.Entries
                .Select(e => new TransactionHistoryEntryResponse(
                    e.Id.Value, e.TransferId.Value, e.Direction.ToString(), e.Amount.Amount, e.Amount.Currency.Code, e.SignedAmount.Amount, e.PostedAt))
                .ToList(),
            page.TotalCount,
            page.PageNumber,
            page.PageSize);

        return Ok(response);
    }
}
