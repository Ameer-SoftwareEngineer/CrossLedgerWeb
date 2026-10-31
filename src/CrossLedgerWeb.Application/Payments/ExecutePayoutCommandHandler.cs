using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Payments;

/// <summary>
/// The orchestration PaymentRoutingEngine.SelectAsync alone doesn't provide (specification
/// 5.3/5.4): reserve funds, pick a route, submit to the winning provider, and fail over
/// through the ranked chain on a deterministic rejection until one accepts or the chain is
/// exhausted. Follows specification 5.5's own state ordering - RESERVED happens before a
/// route is even selected, so a corridor with literally no capable provider still leaves
/// funds reserved (PendingManual), not silently un-debited.
///
/// Deliberately NOT handled here (specification 5.4's "timeout or ambiguous response"
/// path): IPaymentProvider has no way to query a prior attempt's true status by
/// idempotency key, so a transport-level exception from ExecuteAsync propagates instead of
/// being treated as ambiguous-and-safe-to-query. Only a provider's own deterministic
/// PayoutResult.Rejected triggers failover here - that gap is acknowledged, not silently
/// papered over.
/// </summary>
public sealed class ExecutePayoutCommandHandler : IRequestHandler<ExecutePayoutCommand, ExecutePayoutResult>
{
    private readonly IWalletRepository _wallets;
    private readonly IPayoutReserveWalletResolver _reserveWallets;
    private readonly IPayoutRepository _payouts;
    private readonly PaymentRoutingEngine _routingEngine;
    private readonly IReadOnlyDictionary<ProviderCode, IPaymentProvider> _providersByCode;
    private readonly IClock _clock;

    public ExecutePayoutCommandHandler(
        IWalletRepository wallets,
        IPayoutReserveWalletResolver reserveWallets,
        IPayoutRepository payouts,
        PaymentRoutingEngine routingEngine,
        IEnumerable<IPaymentProvider> providers,
        IClock clock)
    {
        _wallets = wallets;
        _reserveWallets = reserveWallets;
        _payouts = payouts;
        _routingEngine = routingEngine;
        _providersByCode = providers.ToDictionary(p => p.Code);
        _clock = clock;
    }

    public async Task<ExecutePayoutResult> Handle(ExecutePayoutCommand request, CancellationToken cancellationToken)
    {
        var sourceWallet = await _wallets.GetByIdAsync(request.SourceWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.SourceWalletId);

        var targetCurrency = Currency.From(request.TargetCurrencyCode);
        var corridor = new Corridor(sourceWallet.Currency, targetCurrency, request.DestinationCountry);
        var amount = new Money(request.Amount, sourceWallet.Currency);

        var reserveWallet = await _reserveWallets.GetPayoutReserveWalletAsync(sourceWallet.Currency, cancellationToken);

        var now = _clock.UtcNow;
        var transferId = TransferId.New();
        var (payout, _) = PayoutReserver.Reserve(PayoutId.New(), transferId, sourceWallet, reserveWallet, amount, now);
        _payouts.Add(payout);

        var routingRequest = new PayoutRequest(transferId, corridor, amount, request.IdempotencyKey, request.Preference);

        RoutingDecision routingDecision;
        try
        {
            routingDecision = await _routingEngine.SelectAsync(routingRequest, cancellationToken);
        }
        catch (NoRouteAvailableException)
        {
            // Funds are already reserved (specification 5.5's own ordering) - a corridor
            // with no capable provider at all needs the same operator attention as a chain
            // that tried everyone and failed, not a silent 4xx that leaves money reserved
            // with no record of why.
            payout.MarkPendingManual();
            return ToResult(payout);
        }

        var candidates = new List<ScoredProviderQuote> { routingDecision.Primary };
        candidates.AddRange(routingDecision.Fallbacks);

        payout.Submit();

        for (var i = 0; i < candidates.Count; i++)
        {
            var provider = _providersByCode[candidates[i].Quote.ProviderCode];
            var result = await provider.ExecuteAsync(routingRequest, cancellationToken);

            if (result.Outcome == PayoutOutcome.Accepted)
            {
                payout.MarkAccepted(provider.Code, result.ProviderReference!);
                return ToResult(payout);
            }

            payout.MarkProviderFailed(provider.Code);

            var isLastCandidate = i == candidates.Count - 1;
            if (isLastCandidate)
                payout.MarkPendingManual();
            else
                payout.Submit();
        }

        return ToResult(payout);
    }

    private static ExecutePayoutResult ToResult(Payout payout) =>
        new(payout.Id, payout.TransferId, payout.State, payout.ProviderCode, payout.ProviderReference);
}
