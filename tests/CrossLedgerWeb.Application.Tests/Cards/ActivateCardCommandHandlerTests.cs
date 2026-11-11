using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class ActivateCardCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<IClock> _clock = new();

    public ActivateCardCommandHandlerTests() => _clock.Setup(x => x.UtcNow).Returns(Now);

    private static Card NewShippedPhysicalCard()
    {
        var card = new Card(
            CardId.New(), Owner, WalletId.New(), Usd, CardType.Physical, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);
        card.Approve();
        card.MarkShipped("UPS", "1Z999", Now);
        return card;
    }

    private ActivateCardCommandHandler CreateHandler() => new(_cards.Object, _clock.Object);

    [Fact]
    public async Task Handle_activates_the_card()
    {
        var card = NewShippedPhysicalCard();
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        await CreateHandler().Handle(new ActivateCardCommand(card.Id, Owner), CancellationToken.None);

        card.IsActivated.Should().BeTrue();
        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Delivered);
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var card = NewShippedPhysicalCard();
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        var act = () => CreateHandler().Handle(new ActivateCardCommand(card.Id, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }
}
