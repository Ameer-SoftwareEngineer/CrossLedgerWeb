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

public class CloseCardCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Card _card;

    public CloseCardCommandHandlerTests()
    {
        _card = new Card(CardId.New(), Owner, WalletId.New(), Usd, "Test Customer", "4532750123456784", "123", 1, 2029, Now);
        _cards.Setup(x => x.GetByIdAsync(_card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_card);
        _clock.Setup(x => x.UtcNow).Returns(Now.AddDays(1));
    }

    private CloseCardCommandHandler CreateHandler() => new(_cards.Object, _clock.Object);

    [Fact]
    public async Task Handle_closes_the_card()
    {
        var handler = CreateHandler();

        await handler.Handle(new CloseCardCommand(_card.Id, Owner), CancellationToken.None);

        _card.Status.Should().Be(CardStatus.Closed);
        _card.ClosedAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(new CloseCardCommand(_card.Id, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_card_is_already_closed()
    {
        _card.Close(Now);
        var handler = CreateHandler();

        var act = () => handler.Handle(new CloseCardCommand(_card.Id, Owner), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCardStateException>();
    }
}
