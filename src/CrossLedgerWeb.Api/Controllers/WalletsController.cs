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
}
