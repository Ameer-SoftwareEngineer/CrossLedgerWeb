namespace CrossLedgerWeb.Api.RealTime;

/// <summary>Wire shape of the "RateUpdated" SignalR message - deliberately not reusing
/// ExchangeRateReading (an Application type) so the Api layer owns exactly what it
/// broadcasts, matching how every other Api-facing shape here is a purpose-built
/// response, not a domain/application type leaking across the boundary.</summary>
public sealed record FxRateUpdate(string FromCurrency, string ToCurrency, decimal MidMarketRate, DateTimeOffset AsOf, bool IsStale);
