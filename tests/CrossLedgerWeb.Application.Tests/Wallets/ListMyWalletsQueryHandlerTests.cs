using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Wallets;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Wallets;

public class ListMyWalletsQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Pkr = Currency.From("PKR");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly Mock<IWalletRepository> _wallets = new();

    [Fact]
    public async Task Handle_returns_a_summary_with_derived_balance_per_wallet()
    {
        var ownerId = UserId.New();
        var usdWallet = new Wallet(WalletId.New(), ownerId, Usd);
        usdWallet.Credit(new Money(120m, Usd), TransferId.New(), Now);
        var pkrWallet = new Wallet(WalletId.New(), ownerId, Pkr);
        pkrWallet.Credit(new Money(5000m, Pkr), TransferId.New(), Now);
        _wallets.Setup(x => x.ListByOwnerAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { usdWallet, pkrWallet });
        var handler = new ListMyWalletsQueryHandler(_wallets.Object);

        var result = await handler.Handle(new ListMyWalletsQuery(ownerId), CancellationToken.None);

        result.Should().BeEquivalentTo(new[]
        {
            new WalletSummary(usdWallet.Id, Usd, new Money(120m, Usd)),
            new WalletSummary(pkrWallet.Id, Pkr, new Money(5000m, Pkr)),
        });
    }

    [Fact]
    public async Task Handle_returns_an_empty_list_when_the_owner_has_no_wallets()
    {
        var ownerId = UserId.New();
        _wallets.Setup(x => x.ListByOwnerAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Wallet>());
        var handler = new ListMyWalletsQueryHandler(_wallets.Object);

        var result = await handler.Handle(new ListMyWalletsQuery(ownerId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
