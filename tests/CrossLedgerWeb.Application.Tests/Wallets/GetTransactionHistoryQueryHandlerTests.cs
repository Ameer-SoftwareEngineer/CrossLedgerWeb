using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Wallets;

public class GetTransactionHistoryQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<ITransactionHistoryReader> _reader = new();

    [Fact]
    public async Task Handle_returns_the_page_when_the_caller_owns_the_wallet()
    {
        var walletId = WalletId.New();
        var callerId = UserId.New();
        _wallets.Setup(x => x.GetOwnerIdAsync(walletId, It.IsAny<CancellationToken>())).ReturnsAsync(callerId);
        var page = new TransactionHistoryPage(
            new[]
            {
                new TransactionHistoryEntry(
                    LedgerEntryId.New(), TransferId.New(), LedgerDirection.Credit,
                    new Money(50m, Usd), new Money(50m, Usd), Now),
            },
            TotalCount: 1, PageNumber: 1, PageSize: 20);
        _reader.Setup(x => x.GetPageAsync(walletId, 1, 20, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(page);
        var handler = new GetTransactionHistoryQueryHandler(_wallets.Object, _reader.Object);

        var result = await handler.Handle(
            new GetTransactionHistoryQuery(walletId, callerId, 1, 20, null, null), CancellationToken.None);

        result.Should().BeSameAs(page);
    }

    [Fact]
    public async Task Handle_throws_wallet_not_found_when_the_wallet_does_not_exist()
    {
        var walletId = WalletId.New();
        _wallets.Setup(x => x.GetOwnerIdAsync(walletId, It.IsAny<CancellationToken>())).ReturnsAsync((UserId?)null);
        var handler = new GetTransactionHistoryQueryHandler(_wallets.Object, _reader.Object);

        var act = () => handler.Handle(
            new GetTransactionHistoryQuery(walletId, UserId.New(), 1, 20, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_wallet_not_found_when_the_caller_does_not_own_the_wallet()
    {
        var walletId = WalletId.New();
        _wallets.Setup(x => x.GetOwnerIdAsync(walletId, It.IsAny<CancellationToken>())).ReturnsAsync(UserId.New());
        var handler = new GetTransactionHistoryQueryHandler(_wallets.Object, _reader.Object);

        var act = () => handler.Handle(
            new GetTransactionHistoryQuery(walletId, UserId.New(), 1, 20, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
        _reader.Verify(x => x.GetPageAsync(
            It.IsAny<WalletId>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
