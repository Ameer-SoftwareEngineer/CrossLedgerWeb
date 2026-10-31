using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Payments;
using MediatR;

namespace CrossLedgerWeb.Application.Payments;

public sealed class ReceivePayoutWebhookCommandHandler : IRequestHandler<ReceivePayoutWebhookCommand, ReceivePayoutWebhookResult>
{
    private readonly IProcessedWebhookEventStore _processedEvents;
    private readonly IPayoutRepository _payouts;
    private readonly IWalletRepository _wallets;
    private readonly IPayoutReserveWalletResolver _reserveWallets;
    private readonly IWebhookNotifier _webhookNotifier;
    private readonly IClock _clock;

    public ReceivePayoutWebhookCommandHandler(
        IProcessedWebhookEventStore processedEvents,
        IPayoutRepository payouts,
        IWalletRepository wallets,
        IPayoutReserveWalletResolver reserveWallets,
        IWebhookNotifier webhookNotifier,
        IClock clock)
    {
        _processedEvents = processedEvents;
        _payouts = payouts;
        _wallets = wallets;
        _reserveWallets = reserveWallets;
        _webhookNotifier = webhookNotifier;
        _clock = clock;
    }

    public async Task<ReceivePayoutWebhookResult> Handle(ReceivePayoutWebhookCommand request, CancellationToken cancellationToken)
    {
        // "Duplicate webhooks - events are deduplicated on provider event id before any
        // ledger entry is written" (specification 5.4) - checked first, before touching
        // the payout at all.
        if (await _processedEvents.HasBeenProcessedAsync(request.ProviderCode, request.EventId, cancellationToken))
            return new ReceivePayoutWebhookResult(WasDuplicate: true, PayoutState: null);

        var payout = await _payouts.GetByProviderReferenceAsync(request.ProviderCode, request.ProviderReference, cancellationToken)
            ?? throw new PayoutNotFoundException(request.ProviderCode, request.ProviderReference);

        var now = _clock.UtcNow;

        // "Out-of-order webhooks - a settled event arriving before a processing event must
        // not regress the transfer state; state transitions are validated against an
        // explicit state machine" (specification 5.4). Payout.MarkSettled/Reverse already
        // enforce this - an out-of-order call throws InvalidPayoutTransitionException
        // exactly like any other illegal transition, with no special-casing needed here.
        if (request.EventType == PayoutWebhookEventType.Settled)
        {
            payout.MarkSettled();
        }
        else
        {
            var sourceWallet = await _wallets.GetByIdAsync(payout.SourceWalletId, cancellationToken)
                ?? throw new WalletNotFoundException(payout.SourceWalletId);
            var reserveWallet = await _reserveWallets.GetPayoutReserveWalletAsync(payout.Amount.Currency, cancellationToken);

            PayoutReserver.ReverseReservation(payout, sourceWallet, reserveWallet, now);
        }

        _processedEvents.MarkProcessed(request.ProviderCode, request.EventId, now);

        // Specification 2.6: notify CrossLedger's own configured subscriber the way
        // Stripe and similar providers notify merchants - only once the payout's new
        // state is final, never for a duplicate we already told them about.
        await _webhookNotifier.NotifyAsync(
            $"payout.{payout.State.ToString().ToLowerInvariant()}",
            new { payoutId = payout.Id.Value, transferId = payout.TransferId.Value, state = payout.State.ToString() },
            cancellationToken);

        return new ReceivePayoutWebhookResult(WasDuplicate: false, payout.State);
    }
}
