using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Fx;

public sealed class GetFxRateOhlcQueryHandler : IRequestHandler<GetFxRateOhlcQuery, IReadOnlyList<FxRateOhlcPoint>>
{
    private readonly IFxRateOhlcReader _reader;

    public GetFxRateOhlcQueryHandler(IFxRateOhlcReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<FxRateOhlcPoint>> Handle(GetFxRateOhlcQuery request, CancellationToken cancellationToken) =>
        _reader.GetDailyAsync(Currency.From(request.FromCurrency), Currency.From(request.ToCurrency), request.FromDate, request.ToDate, cancellationToken);
}
