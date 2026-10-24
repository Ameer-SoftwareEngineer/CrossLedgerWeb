using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class SetCardSpendingLimitCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Card _card;

    public SetCardSpendingLimitCommandHandlerTests()
    {
        _card = new Card(
            CardId.New(), Owner, WalletId.New(), Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);
        _cards.Setup(x => x.GetByIdAsync(_card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_card);
    }

    private SetCardSpendingLimitCommandHandler CreateHandler() => new(_cards.Object);

    [Fact]
    public async Task Handle_sets_the_spending_limit()
    {
        var handler = CreateHandler();

        await handler.Handle(new SetCardSpendingLimitCommand(_card.Id, Owner, 500m), CancellationToken.None);

        _card.SpendingLimit.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_clears_the_spending_limit_when_null()
    {
        _card.SetSpendingLimit(500m);
        var handler = CreateHandler();

        await handler.Handle(new SetCardSpendingLimitCommand(_card.Id, Owner, null), CancellationToken.None);

        _card.SpendingLimit.Should().BeNull();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(new SetCardSpendingLimitCommand(_card.Id, UserId.New(), 500m), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }
}
