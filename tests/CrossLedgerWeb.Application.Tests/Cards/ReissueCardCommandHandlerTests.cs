using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class ReissueCardCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardIssuer> _cardIssuer = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IClock> _clock = new();

    public ReissueCardCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _cardIssuer.Setup(x => x.IssueAsync(It.IsAny<CardIssuanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedCardDetails("4532750199998888", "999", 6, 2031, null, null));
    }

    private static Card NewActiveCard(WalletId cardWalletId)
    {
        var card = new Card(
            CardId.New(), Owner, cardWalletId, Usd, CardType.Physical, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);
        card.Approve();
        return card;
    }

    private ReissueCardCommandHandler CreateHandler() => new(_cards.Object, _cardIssuer.Object, _wallets.Object, _clock.Object);

    [Fact]
    public async Task Handle_closes_the_old_card_and_issues_a_new_one_on_the_same_wallet()
    {
        var walletId = WalletId.New();
        var oldCard = NewActiveCard(walletId);
        var wallet = new Wallet(walletId, Owner, Usd, WalletKind.Card);
        wallet.Credit(new Money(42m, Usd), TransferId.New(), Now.AddDays(-1));

        _cards.Setup(x => x.GetByIdAsync(oldCard.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldCard);
        _wallets.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

        Card? addedCard = null;
        _cards.Setup(x => x.Add(It.IsAny<Card>())).Callback<Card>(c => addedCard = c);

        var result = await CreateHandler().Handle(new ReissueCardCommand(oldCard.Id, Owner, null), CancellationToken.None);

        oldCard.Status.Should().Be(CardStatus.Closed);
        addedCard.Should().NotBeNull();
        addedCard!.CardWalletId.Should().Be(walletId);
        addedCard.ReplacesCardId.Should().Be(oldCard.Id);
        addedCard.Status.Should().Be(CardStatus.PendingApproval);
        result.CardWalletId.Should().Be(walletId);
        result.Balance.Should().Be(42m);
    }

    [Fact]
    public async Task Handle_throws_when_the_old_card_is_already_closed()
    {
        var walletId = WalletId.New();
        var oldCard = NewActiveCard(walletId);
        oldCard.Close(Now);
        _cards.Setup(x => x.GetByIdAsync(oldCard.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldCard);

        var act = () => CreateHandler().Handle(new ReissueCardCommand(oldCard.Id, Owner, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCardStateException>();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var oldCard = NewActiveCard(WalletId.New());
        _cards.Setup(x => x.GetByIdAsync(oldCard.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldCard);

        var act = () => CreateHandler().Handle(new ReissueCardCommand(oldCard.Id, UserId.New(), null), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }
}
