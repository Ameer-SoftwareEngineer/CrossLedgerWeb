using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class TopUpCardCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Card _card;
    private readonly Wallet _cardWallet;
    private readonly Wallet _sourceWallet;

    public TopUpCardCommandHandlerTests()
    {
        _cardWallet = new Wallet(WalletId.New(), Owner, Usd, WalletKind.Card);
        _sourceWallet = new Wallet(WalletId.New(), Owner, Usd);
        _sourceWallet.Credit(new Money(1000m, Usd), TransferId.New(), Now);

        _card = new Card(
            CardId.New(), Owner, _cardWallet.Id, Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);

        _cards.Setup(x => x.GetByIdAsync(_card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_card);
        _wallets.Setup(x => x.GetByIdAsync(_cardWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_cardWallet);
        _wallets.Setup(x => x.GetByIdAsync(_sourceWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_sourceWallet);
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private TopUpCardCommandHandler CreateHandler() => new(_cards.Object, _wallets.Object, _clock.Object);

    private TopUpCardCommand ValidCommand(decimal amount = 100m) =>
        new(_card.Id, Owner, _sourceWallet.Id, amount, "idem-key-1");

    [Fact]
    public async Task Handle_throws_when_the_card_is_still_PendingApproval()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCardStateException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_card_was_rejected()
    {
        _card.Reject();
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCardStateException>();
    }

    [Fact]
    public async Task Handle_tops_up_an_approved_card()
    {
        _card.Approve();
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(100m), CancellationToken.None);

        result.NewCardBalance.Should().Be(100m);
        _cardWallet.Balance.Amount.Should().Be(100m);
        _sourceWallet.Balance.Amount.Should().Be(900m);
    }
}
