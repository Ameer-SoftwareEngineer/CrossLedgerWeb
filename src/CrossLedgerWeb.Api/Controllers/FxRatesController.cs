using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Fx;
using CrossLedgerWeb.Shared.Fx;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

[ApiController]
[Route("api/v1/fx-rates")]
[Authorize(Roles = Roles.Customer)]
public sealed class FxRatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FxRatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Backs the FX Rates screen's 30-day historical chart (specification 9).
    /// Defaults to the trailing 30 days when no range is given.</summary>
    [HttpGet("ohlc")]
    [ProducesResponseType<IReadOnlyList<FxRateOhlcPointResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FxRateOhlcPointResponse>>> GetOhlc(
        [FromQuery] string fromCurrency,
        [FromQuery] string toCurrency,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var effectiveToDate = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveFromDate = fromDate ?? effectiveToDate.AddDays(-30);

        var points = await _mediator.Send(
            new GetFxRateOhlcQuery(fromCurrency, toCurrency, effectiveFromDate, effectiveToDate), cancellationToken);

        var response = points.Select(p => new FxRateOhlcPointResponse(p.Date, p.Open, p.High, p.Low, p.Close)).ToList();
        return Ok(response);
    }
}
