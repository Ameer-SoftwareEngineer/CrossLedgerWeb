using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;

namespace CrossLedgerWeb.Domain.Tests.Cards;

public class CardTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset IssuedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Card NewCard() => new(
        CardId.New(), UserId.New(), WalletId.New(), Usd, "Test Customer", "4532750123456784", "123", 1, 2029, IssuedAt);

    [Fact]
    public void Constructor_starts_the_card_as_Active_with_no_spending_limit()
    {
        var card = NewCard();

        card.Status.Should().Be(CardStatus.Active);
        card.SpendingLimit.Should().BeNull();
        card.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Last4_is_derived_from_the_end_of_the_pan()
    {
        var card = NewCard();

        card.Last4.Should().Be("6784");
    }

    [Fact]
    public void Constructor_rejects_a_blank_cardholder_name()
    {
        var act = () => new Card(CardId.New(), UserId.New(), WalletId.New(), Usd, " ", "4532750123456784", "123", 1, 2029, IssuedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcd12345678")]
    public void Constructor_rejects_an_invalid_pan(string pan)
    {
        var act = () => new Card(CardId.New(), UserId.New(), WalletId.New(), Usd, "Test Customer", pan, "123", 1, 2029, IssuedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcd")]
    public void Constructor_rejects_an_invalid_cvv(string cvv)
    {
        var act = () => new Card(CardId.New(), UserId.New(), WalletId.New(), Usd, "Test Customer", "4532750123456784", cvv, 1, 2029, IssuedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Freeze_transitions_Active_to_Frozen()
    {
        var card = NewCard();

        card.Freeze();

        card.Status.Should().Be(CardStatus.Frozen);
    }

    [Fact]
    public void Freeze_throws_when_the_card_is_not_Active()
    {
        var card = NewCard();
        card.Freeze();

        var act = () => card.Freeze();

        act.Should().Throw<InvalidCardStateException>();
    }

    [Fact]
    public void Unfreeze_transitions_Frozen_back_to_Active()
    {
        var card = NewCard();
        card.Freeze();

        card.Unfreeze();

        card.Status.Should().Be(CardStatus.Active);
    }

    [Fact]
    public void Unfreeze_throws_when_the_card_was_never_frozen()
    {
        var card = NewCard();

        var act = () => card.Unfreeze();

        act.Should().Throw<InvalidCardStateException>();
    }

    [Fact]
    public void Close_is_terminal()
    {
        var card = NewCard();
        card.Close(IssuedAt.AddDays(1));

        card.Status.Should().Be(CardStatus.Closed);
        card.ClosedAt.Should().Be(IssuedAt.AddDays(1));

        var act = () => card.Close(IssuedAt.AddDays(2));
        act.Should().Throw<InvalidCardStateException>();
    }

    [Fact]
    public void Close_works_from_Frozen_too()
    {
        var card = NewCard();
        card.Freeze();

        card.Close(IssuedAt.AddDays(1));

        card.Status.Should().Be(CardStatus.Closed);
    }

    [Fact]
    public void SetSpendingLimit_rejects_a_negative_limit()
    {
        var card = NewCard();

        var act = () => card.SetSpendingLimit(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EnsureCanSpend_throws_when_the_card_is_frozen()
    {
        var card = NewCard();
        card.Freeze();

        var act = () => card.EnsureCanSpend(new Money(10m, Usd));

        act.Should().Throw<InvalidCardStateException>();
    }

    [Fact]
    public void EnsureCanSpend_throws_when_the_card_is_closed()
    {
        var card = NewCard();
        card.Close(IssuedAt);

        var act = () => card.EnsureCanSpend(new Money(10m, Usd));

        act.Should().Throw<InvalidCardStateException>();
    }

    [Fact]
    public void EnsureCanSpend_throws_when_the_amount_exceeds_the_spending_limit()
    {
        var card = NewCard();
        card.SetSpendingLimit(100m);

        var act = () => card.EnsureCanSpend(new Money(100.01m, Usd));

        act.Should().Throw<CardSpendingLimitExceededException>();
    }

    [Fact]
    public void EnsureCanSpend_allows_an_amount_at_exactly_the_spending_limit()
    {
        var card = NewCard();
        card.SetSpendingLimit(100m);

        var act = () => card.EnsureCanSpend(new Money(100m, Usd));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanSpend_allows_any_amount_when_no_limit_is_set()
    {
        var card = NewCard();

        var act = () => card.EnsureCanSpend(new Money(1_000_000m, Usd));

        act.Should().NotThrow();
    }
}
