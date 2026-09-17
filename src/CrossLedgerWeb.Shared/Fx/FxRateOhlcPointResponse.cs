namespace CrossLedgerWeb.Shared.Fx;

public sealed record FxRateOhlcPointResponse(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close);
