using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class FreezeCardCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Card _card;

    public FreezeCardCommandHandlerTests()
    {
        _card = new Card(
            CardId.New(), Owner, WalletId.New(), Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);
        _cards.Setup(x => x.GetByIdAsync(_card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_card);
    }

    private FreezeCardCommandHandler CreateHandler() => new(_cards.Object);

    [Fact]
    public async Task Handle_freezes_the_card()
    {
        var handler = CreateHandler();

        await handler.Handle(new FreezeCardCommand(_card.Id, Owner), CancellationToken.None);

        _card.Status.Should().Be(CardStatus.Frozen);
    }

    [Fact]
    public async Task Handle_throws_when_the_card_does_not_exist()
    {
        _cards.Setup(x => x.GetByIdAsync(It.IsAny<CardId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Card?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new FreezeCardCommand(_card.Id, Owner), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(new FreezeCardCommand(_card.Id, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_card_is_already_frozen()
    {
        _card.Freeze();
        var handler = CreateHandler();

        var act = () => handler.Handle(new FreezeCardCommand(_card.Id, Owner), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCardStateException>();
    }
}
