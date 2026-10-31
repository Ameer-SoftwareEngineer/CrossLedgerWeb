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

public class ExecutePayoutCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IPayoutReserveWalletResolver> _reserveWallets = new();
    private readonly Mock<IPayoutRepository> _payouts = new();
    private readonly Mock<IProviderStatsProvider> _stats = new();
    private readonly Mock<IRoutingAuditLog> _auditLog = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Wallet _sourceWallet;
    private readonly Wallet _reserveWallet;

    public ExecutePayoutCommandHandlerTests()
    {
        _sourceWallet = new Wallet(WalletId.New(), UserId.New(), Usd);
        _sourceWallet.Credit(new Money(1000m, Usd), TransferId.New(), Now);
        _reserveWallet = new Wallet(WalletId.New(), UserId.New(), Usd, WalletKind.PayoutReserve);

        _wallets.Setup(x => x.GetByIdAsync(_sourceWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_sourceWallet);
        _reserveWallets.Setup(x => x.GetPayoutReserveWalletAsync(Usd, It.IsAny<CancellationToken>())).ReturnsAsync(_reserveWallet);
        _stats.Setup(x => x.GetStatsAsync(It.IsAny<ProviderCode>(), It.IsAny<Currency>(), It.IsAny<CancellationToken>())).ReturnsAsync(ProviderStats.Unknown);
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private static Mock<IPaymentProvider> Provider(ProviderCode code, PayoutResult result, ProviderQuote? quote = null)
    {
        var provider = new Mock<IPaymentProvider>();
        provider.SetupGet(x => x.Code).Returns(code);
        provider.Setup(x => x.Supports(It.IsAny<Corridor>())).Returns(true);
        provider.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderHealth(code, ProviderHealthStatus.Healthy, DateTimeOffset.UtcNow));
        provider.Setup(x => x.QuoteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote ?? new ProviderQuote(code, new Money(100m, Usd), new Money(1m, Usd), TimeSpan.FromMinutes(30)));
        provider.Setup(x => x.ExecuteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return provider;
    }

    private ExecutePayoutCommandHandler CreateHandler(params IPaymentProvider[] providers) => new(
        _wallets.Object,
        _reserveWallets.Object,
        _payouts.Object,
        new PaymentRoutingEngine(providers, _stats.Object, new ProviderQuoteScorer(), _auditLog.Object),
        providers,
        _clock.Object);

    private ExecutePayoutCommand ValidCommand(decimal amount = 100m) => new(
        _sourceWallet.Id, "EUR", "DE", amount, RoutingPreference.Standard, "idem-key-1");

    [Fact]
    public async Task Handle_reserves_funds_and_marks_the_payout_processing_when_the_primary_provider_accepts()
    {
        var provider = Provider(ProviderCode.Simulated, new PayoutResult(ProviderCode.Simulated, PayoutOutcome.Accepted, "prov-ref-1", null));
        var handler = CreateHandler(provider.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.State.Should().Be(PayoutState.Processing);
        result.ProviderCode.Should().Be(ProviderCode.Simulated);
        result.ProviderReference.Should().Be("prov-ref-1");
        _sourceWallet.Balance.Should().Be(new Money(900m, Usd));
        _reserveWallet.Balance.Should().Be(new Money(100m, Usd));
        _payouts.Verify(x => x.Add(It.IsAny<Payout>()), Times.Once);
    }

    [Fact]
    public async Task Handle_fails_over_to_the_next_provider_when_the_primary_rejects()
    {
        var rejecting = Provider(ProviderCode.Airwallex, new PayoutResult(ProviderCode.Airwallex, PayoutOutcome.Rejected, null, "declined"),
            new ProviderQuote(ProviderCode.Airwallex, new Money(100m, Usd), new Money(1m, Usd), TimeSpan.FromMinutes(30)));
        var accepting = Provider(ProviderCode.Simulated, new PayoutResult(ProviderCode.Simulated, PayoutOutcome.Accepted, "prov-ref-2", null),
            new ProviderQuote(ProviderCode.Simulated, new Money(100m, Usd), new Money(5m, Usd), TimeSpan.FromMinutes(30)));
        var handler = CreateHandler(rejecting.Object, accepting.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.State.Should().Be(PayoutState.Processing);
        result.ProviderCode.Should().Be(ProviderCode.Simulated);
        rejecting.Verify(x => x.ExecuteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        accepting.Verify(x => x.ExecuteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_marks_pending_manual_when_every_provider_in_the_chain_rejects()
    {
        var first = Provider(ProviderCode.Airwallex, new PayoutResult(ProviderCode.Airwallex, PayoutOutcome.Rejected, null, "declined"));
        var second = Provider(ProviderCode.Simulated, new PayoutResult(ProviderCode.Simulated, PayoutOutcome.Rejected, null, "declined"));
        var handler = CreateHandler(first.Object, second.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.State.Should().Be(PayoutState.PendingManual);
        // Funds stay reserved, not returned - specification 5.4's "funds stay reserved but unspent".
        _sourceWallet.Balance.Should().Be(new Money(900m, Usd));
    }

    [Fact]
    public async Task Handle_marks_pending_manual_when_no_provider_supports_the_corridor()
    {
        var provider = new Mock<IPaymentProvider>();
        provider.SetupGet(x => x.Code).Returns(ProviderCode.Simulated);
        provider.Setup(x => x.Supports(It.IsAny<Corridor>())).Returns(false);
        var handler = CreateHandler(provider.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.State.Should().Be(PayoutState.PendingManual);
        result.ProviderCode.Should().BeNull();
    }

    [Fact]
    public async Task Handle_throws_when_the_source_wallet_does_not_exist()
    {
        _wallets.Setup(x => x.GetByIdAsync(It.IsAny<WalletId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Wallet?)null);
        var provider = Provider(ProviderCode.Simulated, new PayoutResult(ProviderCode.Simulated, PayoutOutcome.Accepted, "ref", null));
        var handler = CreateHandler(provider.Object);

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_propagates_insufficient_funds_from_the_domain_before_any_provider_is_contacted()
    {
        var provider = Provider(ProviderCode.Simulated, new PayoutResult(ProviderCode.Simulated, PayoutOutcome.Accepted, "ref", null));
        var handler = CreateHandler(provider.Object);

        var act = () => handler.Handle(ValidCommand(amount: 5000m), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
        provider.Verify(x => x.ExecuteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
