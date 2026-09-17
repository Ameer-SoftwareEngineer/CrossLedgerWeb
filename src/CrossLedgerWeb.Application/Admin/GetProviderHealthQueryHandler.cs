using CrossLedgerWeb.Application.Payments;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

/// <summary>Calls every registered provider's own CheckHealthAsync live - deliberately
/// not IProviderStatsProvider, which is a routing-score placeholder that always reports
/// full marks (it needs persisted payout-outcome history this project doesn't build yet).
/// This is genuinely live data, one real call per provider, same as PaymentRoutingEngine's
/// own health filter.</summary>
public sealed class GetProviderHealthQueryHandler : IRequestHandler<GetProviderHealthQuery, IReadOnlyList<ProviderHealth>>
{
    private readonly IEnumerable<IPaymentProvider> _providers;

    public GetProviderHealthQueryHandler(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<ProviderHealth>> Handle(GetProviderHealthQuery request, CancellationToken cancellationToken)
    {
        var results = new List<ProviderHealth>();

        foreach (var provider in _providers)
        {
            try
            {
                results.Add(await provider.CheckHealthAsync(cancellationToken));
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested is false)
            {
                // A provider whose health check itself throws (network error, bad
                // config) is exactly as actionable to an operator as one that reports
                // Unavailable - it shouldn't take the rest of the list down with it.
                results.Add(new ProviderHealth(provider.Code, ProviderHealthStatus.Unavailable, DateTimeOffset.UtcNow));
            }
        }

        return results;
    }
}
