using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class GetWalletBalanceQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private readonly Mock<IWalletBalanceReader> _reader = new();

    [Fact]
    public async Task Handle_returns_the_balance_the_reader_reports()
    {
        var walletId = WalletId.New();
        _reader.Setup(x => x.GetBalanceAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletBalance(walletId, Usd, 150.25m));
        var handler = new GetWalletBalanceQueryHandler(_reader.Object);

        var result = await handler.Handle(new GetWalletBalanceQuery(walletId), CancellationToken.None);

        result.Currency.Should().Be(Usd);
        result.Balance.Should().Be(150.25m);
    }

    [Fact]
    public async Task Handle_throws_when_the_wallet_does_not_exist()
    {
        var walletId = WalletId.New();
        _reader.Setup(x => x.GetBalanceAsync(walletId, It.IsAny<CancellationToken>())).ReturnsAsync((WalletBalance?)null);
        var handler = new GetWalletBalanceQueryHandler(_reader.Object);

        var act = () => handler.Handle(new GetWalletBalanceQuery(walletId), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }
}
