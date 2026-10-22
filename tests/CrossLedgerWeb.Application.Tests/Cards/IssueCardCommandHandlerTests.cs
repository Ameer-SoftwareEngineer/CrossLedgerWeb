using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class IssueCardCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly WalletId LinkedWalletId = WalletId.New();

    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardNumberGenerator> _numberGenerator = new();
    private readonly Mock<IClock> _clock = new();

    public IssueCardCommandHandlerTests()
    {
        var wallet = new Wallet(LinkedWalletId, Owner, Usd);
        _wallets.Setup(x => x.GetByIdAsync(LinkedWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _numberGenerator.Setup(x => x.Generate()).Returns(("4532750123456784", "123"));
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private IssueCardCommandHandler CreateHandler() => new(_wallets.Object, _cards.Object, _numberGenerator.Object, _clock.Object);

    private static IssueCardCommand ValidCommand() => new(Owner, LinkedWalletId, "Test Customer");

    [Fact]
    public async Task Handle_issues_a_card_linked_to_the_wallets_currency()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Currency.Should().Be(Usd);
        result.Last4.Should().Be("6784");
        result.Status.Should().Be(CardStatus.Active);
        result.ExpiryYear.Should().Be(Now.Year + 3);
    }

    [Fact]
    public async Task Handle_stages_the_card_for_insertion()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        _cards.Verify(x => x.Add(It.Is<Card>(c => c.Id == result.CardId)), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_wallet_does_not_exist()
    {
        _wallets.Setup(x => x.GetByIdAsync(LinkedWalletId, It.IsAny<CancellationToken>())).ReturnsAsync((Wallet?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_wallet_belongs_to_someone_else()
    {
        var othersWallet = new Wallet(LinkedWalletId, UserId.New(), Usd);
        _wallets.Setup(x => x.GetByIdAsync(LinkedWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(othersWallet);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }
}
