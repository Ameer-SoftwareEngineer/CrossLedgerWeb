using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Wallets;

public class GetWalletBalanceQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly Mock<IWalletRepository> _wallets = new();

    [Fact]
    public async Task Handle_returns_the_wallets_derived_balance()
    {
        var wallet = new Wallet(WalletId.New(), UserId.New(), Usd);
        wallet.Credit(new Money(75m, Usd), TransferId.New(), Now);
        _wallets.Setup(x => x.GetByIdAsync(wallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        var handler = new GetWalletBalanceQueryHandler(_wallets.Object);

        var balance = await handler.Handle(new GetWalletBalanceQuery(wallet.Id, wallet.OwnerId), CancellationToken.None);

        balance.Should().Be(new Money(75m, Usd));
    }

    [Fact]
    public async Task Handle_throws_when_the_wallet_does_not_exist()
    {
        _wallets.Setup(x => x.GetByIdAsync(It.IsAny<WalletId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Wallet?)null);
        var handler = new GetWalletBalanceQueryHandler(_wallets.Object);

        var act = () => handler.Handle(new GetWalletBalanceQuery(WalletId.New(), UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_caller_does_not_own_the_wallet()
    {
        var wallet = new Wallet(WalletId.New(), UserId.New(), Usd);
        _wallets.Setup(x => x.GetByIdAsync(wallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        var handler = new GetWalletBalanceQueryHandler(_wallets.Object);

        var act = () => handler.Handle(new GetWalletBalanceQuery(wallet.Id, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }
}
