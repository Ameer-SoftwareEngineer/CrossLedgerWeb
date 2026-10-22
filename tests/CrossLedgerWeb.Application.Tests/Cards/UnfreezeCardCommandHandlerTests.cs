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

public class UnfreezeCardCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Card _card;

    public UnfreezeCardCommandHandlerTests()
    {
        _card = new Card(CardId.New(), Owner, WalletId.New(), Usd, "Test Customer", "4532750123456784", "123", 1, 2029, Now);
        _card.Freeze();
        _cards.Setup(x => x.GetByIdAsync(_card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_card);
    }

    private UnfreezeCardCommandHandler CreateHandler() => new(_cards.Object);

    [Fact]
    public async Task Handle_unfreezes_the_card()
    {
        var handler = CreateHandler();

        await handler.Handle(new UnfreezeCardCommand(_card.Id, Owner), CancellationToken.None);

        _card.Status.Should().Be(CardStatus.Active);
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(new UnfreezeCardCommand(_card.Id, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_card_is_not_frozen()
    {
        _card.Unfreeze();
        var handler = CreateHandler();

        var act = () => handler.Handle(new UnfreezeCardCommand(_card.Id, Owner), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCardStateException>();
    }
}
