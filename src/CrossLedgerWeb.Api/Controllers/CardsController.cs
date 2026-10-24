using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Shared.Cards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

/// <summary>Dev-simulated virtual cards (specification 9's Ibanera-style dashboard) - no
/// real card network or issuing processor sits behind any of this. Reveal and spending
/// limit changes always require a fresh step-up (StepUpOperation.RevealCardDetails /
/// ChangeSpendingLimits), same unconditional gating DisableTwoFactor uses - neither
/// passes ThresholdSetting, so RequireStepUpFilter challenges every call regardless of
/// amount.</summary>
[ApiController]
[Route("api/v1/cards")]
[Authorize(Roles = Roles.Customer)]
public sealed class CardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType<CardResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CardResponse>> Issue(IssueCardRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var command = new IssueCardCommand(
            userId,
            request.Currency,
            Enum.Parse<CardType>(request.CardType, ignoreCase: true),
            request.CardholderName,
            Enum.Parse<SourceOfFunds>(request.SourceOfFunds, ignoreCase: true),
            request.EstimatedGrossAnnualIncome,
            request.EstimatedNetWorth,
            request.IsActivelyEmployed,
            request.EmployerCompanyName,
            request.JobTitle,
            ToDomainAddress(request.RegisteredAddress),
            request.ShippingAddress is null ? null : ToDomainAddress(request.ShippingAddress));

        var result = await _mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ToResponse(
            result.CardId, result.CardWalletId, result.Currency, result.CardType, result.CardholderName,
            result.Last4, result.ExpiryMonth, result.ExpiryYear, result.Status, null, result.Balance, result.IssuedAt));
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CardResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CardResponse>>> ListMine(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var cards = await _mediator.Send(new ListMyCardsQuery(userId), cancellationToken);

        var response = cards.Select(c => ToResponse(
            c.CardId, c.CardWalletId, c.Currency, c.CardType, c.CardholderName, c.Last4,
            c.ExpiryMonth, c.ExpiryYear, c.Status, c.SpendingLimit, c.Balance, c.IssuedAt)).ToList();

        return Ok(response);
    }

    /// <summary>Moves money from one of the caller's own wallets into their card's
    /// dedicated balance - gated the same way a regular transfer is (step-up above
    /// Limits:StepUpAbove), since it's still money leaving a wallet.</summary>
    [HttpPost("{cardId:guid}/top-up")]
    [RequireStepUp(Operation = StepUpOperation.HighValueTransfer, ThresholdSetting = "Limits:StepUpAbove")]
    [ProducesResponseType<TopUpCardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TopUpCardResponse>> TopUp(
        Guid cardId,
        TopUpCardRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new TopUpCardCommand(new CardId(cardId), userId, new WalletId(request.SourceWalletId), request.Amount, idempotencyKey),
            cancellationToken);

        var response = new TopUpCardResponse(
            result.TransferId.Value, result.Amount.Amount, result.Amount.Currency.Code, result.NewCardBalance, result.PostedAt);

        return Ok(response);
    }

    [HttpGet("{cardId:guid}/reveal")]
    [RequireStepUp(Operation = StepUpOperation.RevealCardDetails)]
    [ProducesResponseType<RevealCardDetailsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RevealCardDetailsResponse>> Reveal(Guid cardId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new RevealCardDetailsQuery(new CardId(cardId), userId), cancellationToken);

        return Ok(new RevealCardDetailsResponse(result.Pan, result.Cvv, result.ExpiryMonth, result.ExpiryYear));
    }

    [HttpPost("{cardId:guid}/freeze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Freeze(Guid cardId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _mediator.Send(new FreezeCardCommand(new CardId(cardId), userId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{cardId:guid}/unfreeze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unfreeze(Guid cardId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _mediator.Send(new UnfreezeCardCommand(new CardId(cardId), userId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{cardId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Close(Guid cardId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _mediator.Send(new CloseCardCommand(new CardId(cardId), userId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{cardId:guid}/spending-limit")]
    [RequireStepUp(Operation = StepUpOperation.ChangeSpendingLimits)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSpendingLimit(Guid cardId, SetCardSpendingLimitRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _mediator.Send(new SetCardSpendingLimitCommand(new CardId(cardId), userId, request.Limit), cancellationToken);
        return NoContent();
    }

    /// <summary>No real merchant or card network is involved - this just posts the same
    /// shape of ledger entries a real purchase would, so it shows up in Transaction
    /// History like any other debit.</summary>
    [HttpPost("{cardId:guid}/purchases")]
    [ProducesResponseType<CardPurchaseResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CardPurchaseResponse>> SimulatePurchase(
        Guid cardId,
        SimulateCardPurchaseRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new SimulateCardPurchaseCommand(new CardId(cardId), userId, request.MerchantName, request.Amount, idempotencyKey),
            cancellationToken);

        var response = new CardPurchaseResponse(
            result.PurchaseId.Value, result.TransferId.Value, result.MerchantName,
            result.Amount.Amount, result.Amount.Currency.Code, result.PostedAt);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{cardId:guid}/purchases")]
    [ProducesResponseType<IReadOnlyList<CardPurchaseResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CardPurchaseResponse>>> ListPurchases(Guid cardId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var purchases = await _mediator.Send(new ListCardPurchasesQuery(new CardId(cardId), userId), cancellationToken);

        var response = purchases
            .Select(p => new CardPurchaseResponse(p.PurchaseId.Value, p.TransferId.Value, p.MerchantName, p.Amount.Amount, p.Amount.Currency.Code, p.PostedAt))
            .ToList();

        return Ok(response);
    }

    private static CardResponse ToResponse(
        CardId cardId, WalletId cardWalletId, Currency currency, CardType cardType, string cardholderName,
        string last4, int expiryMonth, int expiryYear, CardStatus status, decimal? spendingLimit, decimal balance, DateTimeOffset issuedAt) =>
        new(cardId.Value, cardWalletId.Value, currency.Code, cardType.ToString(), cardholderName, last4, expiryMonth, expiryYear, status.ToString(), spendingLimit, balance, issuedAt);

    private static Domain.Cards.CardAddress ToDomainAddress(CardAddressDto dto) =>
        new(dto.Street, dto.Number, dto.State, dto.Postcode, dto.City, dto.Country);
}
