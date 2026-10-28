using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ManualTransferCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Pkr = Currency.From("PKR");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IFxSettlementWalletResolver> _fxSettlementWallets = new();
    private readonly Mock<IManualTransferPoster> _poster = new();
    private readonly Mock<IClock> _clock = new();

    private readonly WalletId _sourceWalletId = WalletId.New();
    private readonly WalletId _targetWalletId = WalletId.New();
    private readonly Wallet _fxSettlementUsd = new(WalletId.New(), UserId.New(), Usd, WalletKind.SystemClearing);
    private readonly Wallet _fxSettlementPkr = new(WalletId.New(), UserId.New(), Pkr, WalletKind.SystemClearing);

    public ManualTransferCommandHandlerTests()
    {
        _wallets.Setup(x => x.GetOwnershipAsync(_sourceWalletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletOwnership(UserId.New(), Usd));
        _wallets.Setup(x => x.GetOwnershipAsync(_targetWalletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletOwnership(UserId.New(), Pkr));
        _fxSettlementWallets.Setup(x => x.GetSettlementWalletAsync(Usd, It.IsAny<CancellationToken>())).ReturnsAsync(_fxSettlementUsd);
        _fxSettlementWallets.Setup(x => x.GetSettlementWalletAsync(Pkr, It.IsAny<CancellationToken>())).ReturnsAsync(_fxSettlementPkr);
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private ManualTransferCommandHandler CreateHandler() => new(_wallets.Object, _fxSettlementWallets.Object, _poster.Object, _clock.Object);

    private ManualTransferCommand ValidCommand() => new(_sourceWalletId, _targetWalletId, 100m, 27_990m, "idem-key-1");

    [Fact]
    public async Task Handle_posts_through_the_resolved_settlement_wallets_and_returns_the_result()
    {
        var transferId = TransferId.New();
        _poster
            .Setup(x => x.PostAsync(
                It.IsAny<TransferId>(), _sourceWalletId, _fxSettlementUsd.Id, _fxSettlementPkr.Id, _targetWalletId,
                100m, Usd, 27_990m, Pkr, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManualTransferResult(transferId, "COMPLETED", Now));
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.TransferId.Should().Be(transferId);
        result.PostedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_throws_when_the_source_wallet_does_not_exist()
    {
        _wallets.Setup(x => x.GetOwnershipAsync(_sourceWalletId, It.IsAny<CancellationToken>())).ReturnsAsync((WalletOwnership?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_propagates_insufficient_funds_from_the_poster()
    {
        _poster
            .Setup(x => x.PostAsync(
                It.IsAny<TransferId>(), It.IsAny<WalletId>(), It.IsAny<WalletId>(), It.IsAny<WalletId>(), It.IsAny<WalletId>(),
                It.IsAny<decimal>(), It.IsAny<Currency>(), It.IsAny<decimal>(), It.IsAny<Currency>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsufficientFundsException(_sourceWalletId, new Money(0m, Usd), new Money(100m, Usd)));
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }
}
