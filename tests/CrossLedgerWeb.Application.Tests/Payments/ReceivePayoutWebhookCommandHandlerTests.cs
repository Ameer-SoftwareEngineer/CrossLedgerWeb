using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Payments;

public class ReceivePayoutWebhookCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private const string ProviderReference = "prov-ref-1";
    private const string EventId = "evt-1";

    private readonly Mock<IProcessedWebhookEventStore> _processedEvents = new();
    private readonly Mock<IPayoutRepository> _payouts = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IPayoutReserveWalletResolver> _reserveWallets = new();
    private readonly Mock<IWebhookNotifier> _webhookNotifier = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Wallet _sourceWallet;
    private readonly Wallet _reserveWallet;

    public ReceivePayoutWebhookCommandHandlerTests()
    {
        _sourceWallet = new Wallet(WalletId.New(), UserId.New(), Usd);
        _sourceWallet.Credit(new Money(1000m, Usd), TransferId.New(), Now);
        _reserveWallet = new Wallet(WalletId.New(), UserId.New(), Usd, WalletKind.PayoutReserve);

        _wallets.Setup(x => x.GetByIdAsync(_sourceWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_sourceWallet);
        _reserveWallets.Setup(x => x.GetPayoutReserveWalletAsync(Usd, It.IsAny<CancellationToken>())).ReturnsAsync(_reserveWallet);
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _processedEvents.Setup(x => x.HasBeenProcessedAsync(It.IsAny<ProviderCode>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private Payout PayoutInProcessing()
    {
        var (payout, _) = PayoutReserver.Reserve(PayoutId.New(), TransferId.New(), _sourceWallet, _reserveWallet, new Money(100m, Usd), Now);
        payout.Submit();
        payout.MarkAccepted(ProviderCode.Simulated, ProviderReference);
        return payout;
    }

    private ReceivePayoutWebhookCommandHandler CreateHandler() => new(
        _processedEvents.Object, _payouts.Object, _wallets.Object, _reserveWallets.Object, _webhookNotifier.Object, _clock.Object);

    private static ReceivePayoutWebhookCommand Command(PayoutWebhookEventType eventType) => new(
        ProviderCode.Simulated, EventId, ProviderReference, eventType);

    [Fact]
    public async Task Handle_returns_early_without_touching_the_payout_when_the_event_was_already_processed()
    {
        _processedEvents.Setup(x => x.HasBeenProcessedAsync(ProviderCode.Simulated, EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(PayoutWebhookEventType.Settled), CancellationToken.None);

        result.WasDuplicate.Should().BeTrue();
        _payouts.Verify(x => x.GetByProviderReferenceAsync(It.IsAny<ProviderCode>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_settled_event_marks_the_payout_settled()
    {
        var payout = PayoutInProcessing();
        _payouts.Setup(x => x.GetByProviderReferenceAsync(ProviderCode.Simulated, ProviderReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(PayoutWebhookEventType.Settled), CancellationToken.None);

        result.WasDuplicate.Should().BeFalse();
        result.PayoutState.Should().Be(PayoutState.Settled);
        _processedEvents.Verify(x => x.MarkProcessed(ProviderCode.Simulated, EventId, Now), Times.Once);
        _webhookNotifier.Verify(x => x.NotifyAsync("payout.settled", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_settled_event_arriving_out_of_order_is_rejected_not_silently_applied()
    {
        // Still Submitted (never accepted) - a "settled" webhook here would regress the
        // state machine (specification 5.4's out-of-order protection).
        var (payout, _) = PayoutReserver.Reserve(PayoutId.New(), TransferId.New(), _sourceWallet, _reserveWallet, new Money(100m, Usd), Now);
        payout.Submit();
        _payouts.Setup(x => x.GetByProviderReferenceAsync(ProviderCode.Simulated, ProviderReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);
        var handler = CreateHandler();

        var act = () => handler.Handle(Command(PayoutWebhookEventType.Settled), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPayoutTransitionException>();
    }

    [Fact]
    public async Task A_failed_event_reverses_the_reservation_and_returns_funds_to_the_customer()
    {
        var payout = PayoutInProcessing();
        _payouts.Setup(x => x.GetByProviderReferenceAsync(ProviderCode.Simulated, ProviderReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(PayoutWebhookEventType.Failed), CancellationToken.None);

        result.PayoutState.Should().Be(PayoutState.Reversed);
        _sourceWallet.Balance.Should().Be(new Money(1000m, Usd));
        _reserveWallet.Balance.Should().Be(new Money(0m, Usd));
    }

    [Fact]
    public async Task Handle_throws_when_no_payout_matches_the_provider_reference()
    {
        _payouts.Setup(x => x.GetByProviderReferenceAsync(It.IsAny<ProviderCode>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payout?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(Command(PayoutWebhookEventType.Settled), CancellationToken.None);

        await act.Should().ThrowAsync<PayoutNotFoundException>();
    }
}
