using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class SetCardSpendingControlsCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Card _card;

    public SetCardSpendingControlsCommandHandlerTests()
    {
        _card = new Card(
            CardId.New(), Owner, WalletId.New(), Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);
        _cards.Setup(x => x.GetByIdAsync(_card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_card);
    }

    private SetCardSpendingControlsCommandHandler CreateHandler() => new(_cards.Object);

    [Fact]
    public async Task Handle_sets_the_blocked_categories_allowed_countries_and_velocity_limits()
    {
        var handler = CreateHandler();
        var blocked = new HashSet<string> { "7995" };
        var allowed = new HashSet<string> { "US", "CA" };

        await handler.Handle(new SetCardSpendingControlsCommand(_card.Id, Owner, blocked, allowed, 100m, 1000m), CancellationToken.None);

        _card.BlockedMerchantCategoryCodes.Should().BeEquivalentTo(blocked);
        _card.AllowedCountries.Should().BeEquivalentTo(allowed);
        _card.DailySpendingLimit.Should().Be(100m);
        _card.MonthlySpendingLimit.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_clears_controls_when_given_empty_sets_and_null_limits()
    {
        _card.SetSpendingControls(new HashSet<string> { "7995" }, new HashSet<string> { "US" }, 100m, 1000m);
        var handler = CreateHandler();

        await handler.Handle(
            new SetCardSpendingControlsCommand(_card.Id, Owner, new HashSet<string>(), new HashSet<string>(), null, null),
            CancellationToken.None);

        _card.BlockedMerchantCategoryCodes.Should().BeEmpty();
        _card.AllowedCountries.Should().BeEmpty();
        _card.DailySpendingLimit.Should().BeNull();
        _card.MonthlySpendingLimit.Should().BeNull();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new SetCardSpendingControlsCommand(_card.Id, UserId.New(), new HashSet<string>(), new HashSet<string>(), null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }
}
