using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class IssueCardCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardNumberGenerator> _numberGenerator = new();
    private readonly Mock<IClock> _clock = new();

    public IssueCardCommandHandlerTests()
    {
        _numberGenerator.Setup(x => x.Generate()).Returns(("4532750123456784", "123"));
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private IssueCardCommandHandler CreateHandler() => new(_wallets.Object, _cards.Object, _numberGenerator.Object, _clock.Object);

    private static IssueCardCommand ValidCommand(CardAddress? shippingAddress = null) => new(
        Owner, "USD", CardType.Virtual, "Test Customer", SourceOfFunds.Salary, 5000m, 100000m, true,
        "Acme Inc", "Developer", TestAddress, shippingAddress);

    [Fact]
    public async Task Handle_issues_a_card_with_a_fresh_zero_balance_card_wallet()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Currency.Should().Be(Currency.From("USD"));
        result.CardType.Should().Be(CardType.Virtual);
        result.Last4.Should().Be("6784");
        result.Status.Should().Be(CardStatus.Active);
        result.Balance.Should().Be(0m);
        result.ExpiryYear.Should().Be(Now.Year + 3);
    }

    [Fact]
    public async Task Handle_stages_both_the_card_wallet_and_the_card_for_insertion()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        _wallets.Verify(x => x.Add(It.Is<Wallet>(w => w.Id == result.CardWalletId && w.Kind == WalletKind.Card)), Times.Once);
        _cards.Verify(x => x.Add(It.Is<Card>(c => c.Id == result.CardId && c.CardWalletId == result.CardWalletId)), Times.Once);
    }

    [Fact]
    public async Task Handle_defaults_the_shipping_address_to_the_registered_address_when_not_given()
    {
        var handler = CreateHandler();
        Card? staged = null;
        _cards.Setup(x => x.Add(It.IsAny<Card>())).Callback<Card>(c => staged = c);

        await handler.Handle(ValidCommand(shippingAddress: null), CancellationToken.None);

        staged.Should().NotBeNull();
        staged!.ShippingAddress.Should().Be(TestAddress);
    }
}
