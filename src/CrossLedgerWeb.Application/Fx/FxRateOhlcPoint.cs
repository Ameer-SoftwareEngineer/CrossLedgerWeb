namespace CrossLedgerWeb.Application.Fx;

/// <summary>One day's aggregated rate range - open/high/low/close, matching
/// usp_GetFxRateOHLC's own columns (specification 4.1).</summary>
public sealed record FxRateOhlcPoint(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close);
