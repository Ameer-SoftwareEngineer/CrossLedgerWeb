using CrossLedgerWeb.Application.Cards;
using MediatR;

namespace CrossLedgerWeb.Api.BackgroundJobs;

/// <summary>The expiry sweeper spec 12.3/12.4's "Settlement" week calls for - releases every
/// real-time authorization hold whose window lapsed without a capture ever arriving. Same
/// PeriodicTimer shape as <see cref="LedgerReconciliationBackgroundService"/>, hourly rather
/// than daily since a stuck hold directly blocks a customer's own available balance (a
/// reconciliation imbalance is a backend incident; an uncaptured hold is user-facing).</summary>
public sealed class ExpireCardAuthorizationHoldsBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpireCardAuthorizationHoldsBackgroundService> _logger;

    public ExpireCardAuthorizationHoldsBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ExpireCardAuthorizationHoldsBackgroundService> logger)
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
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var releasedCount = await mediator.Send(new ReleaseExpiredCardAuthorizationHoldsCommand(), cancellationToken);

            if (releasedCount > 0)
                _logger.LogInformation("Card authorization expiry sweep released {ReleasedCount} uncaptured hold(s).", releasedCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Card authorization expiry sweep run failed.");
        }
    }
}
