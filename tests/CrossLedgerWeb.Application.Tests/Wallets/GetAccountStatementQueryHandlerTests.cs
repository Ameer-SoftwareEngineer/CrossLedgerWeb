using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Wallets;

public class GetAccountStatementQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IAccountStatementReader> _reader = new();

    private GetAccountStatementQueryHandler CreateHandler() => new(_wallets.Object, _reader.Object);

    [Fact]
    public async Task Handle_returns_the_statement_when_the_caller_owns_the_wallet()
    {
        var walletId = WalletId.New();
        var callerId = UserId.New();
        _wallets.Setup(x => x.GetOwnershipAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletOwnership(callerId, Usd));
        var statement = new AccountStatement(
            Usd, 100m,
            [new AccountStatementEntry(LedgerEntryId.New(), TransferId.New(), LedgerDirection.Credit, new Money(50m, Usd), Now, 150m)]);
        _reader.Setup(x => x.GetStatementAsync(walletId, Usd, Now.AddDays(-30), Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetAccountStatementQuery(walletId, callerId, Now.AddDays(-30), Now), CancellationToken.None);

        result.Should().BeSameAs(statement);
    }

    [Fact]
    public async Task Handle_throws_wallet_not_found_when_the_wallet_does_not_exist()
    {
        var walletId = WalletId.New();
        _wallets.Setup(x => x.GetOwnershipAsync(walletId, It.IsAny<CancellationToken>())).ReturnsAsync((WalletOwnership?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new GetAccountStatementQuery(walletId, UserId.New(), Now.AddDays(-30), Now), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_wallet_not_found_when_the_caller_does_not_own_the_wallet()
    {
        var walletId = WalletId.New();
        _wallets.Setup(x => x.GetOwnershipAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletOwnership(UserId.New(), Usd));
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new GetAccountStatementQuery(walletId, UserId.New(), Now.AddDays(-30), Now), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
        _reader.Verify(x => x.GetStatementAsync(
            It.IsAny<WalletId>(), It.IsAny<Currency>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
