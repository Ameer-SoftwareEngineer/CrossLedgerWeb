using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace CrossLedgerWeb.Api.RealTime;

/// <summary>Pushes current mid-market rates for a small fixed set of pairs to every
/// connected FxRateHub client (specification 9's live rate ticker). Reads through the
/// same IExchangeRateProvider - Polly-resilient, IMemoryCache-backed with a one-hour TTL
/// (specification 2.5) - that quotes are built from, so this never makes its own direct
/// provider calls and a tick that lands inside the cache window costs nothing.</summary>
public sealed class FxRateBroadcastService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private static readonly IReadOnlyList<(string From, string To)> TickerPairs =
    [
        ("USD", "EUR"),
        ("USD", "GBP"),
        ("EUR", "GBP"),
        ("USD", "CAD"),
    ];

    private readonly IExchangeRateProvider _provider;
    private readonly IHubContext<FxRateHub> _hub;
    private readonly ILogger<FxRateBroadcastService> _logger;

    public FxRateBroadcastService(IExchangeRateProvider provider, IHubContext<FxRateHub> hub, ILogger<FxRateBroadcastService> logger)
    {
        _provider = provider;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            foreach (var (from, to) in TickerPairs)
            {
                await BroadcastOneAsync(from, to, stoppingToken);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task BroadcastOneAsync(string from, string to, CancellationToken cancellationToken)
    {
        try
        {
            var reading = await _provider.GetRateAsync(Currency.From(from), Currency.From(to), cancellationToken);
            var update = new FxRateUpdate(reading.From.Code, reading.To.Code, reading.MidMarketRate, reading.AsOf, reading.IsStale);
            await _hub.Clients.All.SendAsync("RateUpdated", update, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A single pair failing (provider outage, missing config) never stops the
            // ticker for every other pair - it just sits out this tick and tries again
            // on the next one.
            _logger.LogWarning(ex, "Failed to refresh the FX ticker rate for {From}->{To}.", from, to);
        }
    }
}
