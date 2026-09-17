using CrossLedgerWeb.Application.Fx;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by usp_GetFxRateOHLC (specification 4.1) - a window-function
/// aggregation over raw rate snapshots that has no clean LINQ equivalent, so this reads
/// through Dapper rather than EF Core, same as the other reporting readers.</summary>
public interface IFxRateOhlcReader
{
    Task<IReadOnlyList<FxRateOhlcPoint>> GetDailyAsync(
        Currency from, Currency to, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
