using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Transfers;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Transfers;

// "The handler depends on interfaces only. Moq supplies the repository; domain rules
// are tested with no infrastructure at all." - specification section 11.
public class CreateTransferCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Pkr = Currency.From("PKR");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IQuoteRepository> _quotes = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IFxSettlementWalletResolver> _fxSettlementWallets = new();
    private readonly Mock<ITransferLimitsReader> _transferLimits = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Wallet _sourceWallet;
    private readonly Wallet _targetWallet;
    private readonly Wallet _fxSettlementUsd;
    private readonly Wallet _fxSettlementPkr;
    private readonly Quote _quote;

    public CreateTransferCommandHandlerTests()
    {
        _sourceWallet = new Wallet(WalletId.New(), UserId.New(), Usd);
        _sourceWallet.Credit(new Money(100m, Usd), TransferId.New(), Now);
        _targetWallet = new Wallet(WalletId.New(), UserId.New(), Pkr);
        _fxSettlementUsd = new Wallet(WalletId.New(), UserId.New(), Usd, WalletKind.SystemClearing);
        _fxSettlementPkr = new Wallet(WalletId.New(), UserId.New(), Pkr, WalletKind.SystemClearing);
        _quote = new Quote(QuoteId.New(), Usd, Pkr, midMarketRate: 279.90m, spreadRate: 0.005m, Now, TimeSpan.FromSeconds(30));

        _quotes.Setup(x => x.GetByIdAsync(_quote.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_quote);
        _wallets.Setup(x => x.GetByIdAsync(_sourceWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_sourceWallet);
        _wallets.Setup(x => x.GetByIdAsync(_targetWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_targetWallet);
        _fxSettlementWallets.Setup(x => x.GetSettlementWalletAsync(Usd, It.IsAny<CancellationToken>())).ReturnsAsync(_fxSettlementUsd);
        _fxSettlementWallets.Setup(x => x.GetSettlementWalletAsync(Pkr, It.IsAny<CancellationToken>())).ReturnsAsync(_fxSettlementPkr);
        _transferLimits
            .Setup(x => x.CheckAsync(It.IsAny<UserId>(), It.IsAny<Currency>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransferLimitCheck(0m, 0m, 10_000m, ExceedsLimit: false));
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private CreateTransferCommandHandler CreateHandler() => new(
        _quotes.Object, _wallets.Object, _fxSettlementWallets.Object, _transferLimits.Object, _clock.Object);

    private CreateTransferCommand ValidCommand(decimal sourceAmount = 100m) => new(
        _quote.Id, _sourceWallet.Id, _targetWallet.Id, sourceAmount, "idem-key-1");

    [Fact]
    public async Task Handle_posts_the_transfer_and_returns_the_converted_amounts()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.SourceAmount.Should().Be(new Money(100m, Usd));
        result.TargetAmount.Should().Be(_quote.Convert(new Money(100m, Usd), Now));
        result.PostedAt.Should().Be(Now);
        _targetWallet.Balance.Should().Be(result.TargetAmount);
    }

    [Fact]
    public async Task Handle_throws_when_the_quote_does_not_exist()
    {
        _quotes.Setup(x => x.GetByIdAsync(It.IsAny<QuoteId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Quote?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<QuoteNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_source_wallet_does_not_exist()
    {
        _wallets.Setup(x => x.GetByIdAsync(_sourceWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Wallet?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_target_wallet_does_not_exist()
    {
        _wallets.Setup(x => x.GetByIdAsync(_targetWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Wallet?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_propagates_insufficient_funds_from_the_domain()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(sourceAmount: 500m), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_rolling_transfer_limit_is_exceeded()
    {
        _transferLimits
            .Setup(x => x.CheckAsync(_sourceWallet.OwnerId, Usd, 100m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransferLimitCheck(9_950m, 10_050m, 10_000m, ExceedsLimit: true));
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<TransferLimitExceededException>();
    }

    [Fact]
    public async Task Handle_propagates_quote_expired_from_the_domain()
    {
        var expiredClockValue = _quote.ExpiresAt.AddSeconds(1);
        _clock.Setup(x => x.UtcNow).Returns(expiredClockValue);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<QuoteExpiredException>();
    }
}
