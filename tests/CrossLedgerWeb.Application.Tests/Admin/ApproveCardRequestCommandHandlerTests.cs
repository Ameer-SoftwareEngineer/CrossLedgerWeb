using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ApproveCardRequestCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();

    private static Card NewPendingCard() => new(
        CardId.New(), UserId.New(), WalletId.New(), Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
        SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);

    private ApproveCardRequestCommandHandler CreateHandler() => new(_cards.Object);

    [Fact]
    public async Task Handle_approves_the_card()
    {
        var card = NewPendingCard();
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        await CreateHandler().Handle(new ApproveCardRequestCommand(card.Id), CancellationToken.None);

        card.Status.Should().Be(CardStatus.Active);
    }

    [Fact]
    public async Task Handle_throws_when_the_card_does_not_exist()
    {
        _cards.Setup(x => x.GetByIdAsync(It.IsAny<CardId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Card?)null);

        var act = () => CreateHandler().Handle(new ApproveCardRequestCommand(CardId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }
}
