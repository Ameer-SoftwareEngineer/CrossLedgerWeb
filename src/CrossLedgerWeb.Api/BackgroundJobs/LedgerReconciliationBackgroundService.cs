using CrossLedgerWeb.Application.Admin;
using MediatR;

namespace CrossLedgerWeb.Api.BackgroundJobs;

/// <summary>Specification 2.1/4.1's double-entry integrity check, now actually scheduled
/// rather than only reachable through the admin console's on-demand "Run integrity check"
/// button. Runs once at startup and every 24 hours after, same PeriodicTimer shape as
/// <see cref="CrossLedgerWeb.Api.RealTime.FxRateBroadcastService"/>. An imbalance is
/// logged as an error so it surfaces in whatever log aggregation watches this process -
/// usp_ReconcileLedger's own doc comment already calls this out: "a scheduled caller can
/// both log the detail and treat a non-zero exit as an incident."</summary>
public sealed class LedgerReconciliationBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LedgerReconciliationBackgroundService> _logger;

    public LedgerReconciliationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<LedgerReconciliationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunOnceAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ReconcileLedgerQueryHandler resolves to a Scoped reader, so a scope has to
            // be created per run - this service itself is a singleton for the lifetime
            // of the process.
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var imbalances = await mediator.Send(new ReconcileLedgerQuery(), cancellationToken);

            if (imbalances.Count == 0)
            {
                _logger.LogInformation("Nightly ledger reconciliation: every currency is balanced.");
                return;
            }

            foreach (var imbalance in imbalances)
            {
                _logger.LogError(
                    "Nightly ledger reconciliation found a LEDGER_IMBALANCE for {CurrencyCode}: net {NetAmount}.",
                    imbalance.CurrencyCode, imbalance.NetAmount);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed run (DB hiccup, etc.) never stops future runs - it just sits out
            // this tick and tries again on the next one, same as FxRateBroadcastService.
            _logger.LogWarning(ex, "Nightly ledger reconciliation run failed.");
        }
    }
}
