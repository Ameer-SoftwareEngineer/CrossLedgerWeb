using MediatR;

namespace CrossLedgerWeb.Application.Fx;

public sealed record GetFxRateOhlcQuery(string FromCurrency, string ToCurrency, DateOnly FromDate, DateOnly ToDate)
    : IRequest<IReadOnlyList<FxRateOhlcPoint>>;
