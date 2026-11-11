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
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private static Card NewPendingCard(string cardholderName = "Test Customer", string pan = "4532750123456784", string cvv = "123") => new(
        CardId.New(), UserId.New(), WalletId.New(), Usd, CardType.Virtual, cardholderName, pan, cvv, 1, 2029, IssuedAt,
        SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);

    private static Card NewPhysicalCard()
    {
        var card = new Card(
            CardId.New(), UserId.New(), WalletId.New(), Usd, CardType.Physical, "Test Customer", "4532750123456784", "123", 1, 2029, IssuedAt,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);
        card.Approve();
        return card;
    }

    // Most tests below exercise behaviour (freeze, spend, close...) that assumes an
    // approved card - the approval gate itself is covered separately below.
    private static Card NewCard(string cardholderName = "Test Customer", string pan = "4532750123456784", string cvv = "123")
    {
        var card = NewPendingCard(cardholderName, pan, cvv);
        card.Approve();
        return card;
    }

    [Fact]
    public void Constructor_starts_the_card_as_PendingApproval_with_no_spending_limit()
    {
        var card = NewPendingCard();

        card.Status.Should().Be(CardStatus.PendingApproval);
        card.SpendingLimit.Should().BeNull();
        card.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Approve_transitions_PendingApproval_to_Active()
    {
        var card = NewPendingCard();

        card.Approve();

        card.Status.Should().Be(CardStatus.Active);
    }

    [Fact]
    public void Approve_throws_when_the_card_is_not_PendingApproval()
    {
        var card = NewCard();

        var act = () => card.Approve();

        act.Should().Throw<InvalidCardStateException>();
    }

    [Fact]
    public void Reject_transitions_PendingApproval_to_Rejected()
    {
        var card = NewPendingCard();

        card.Reject();

        card.Status.Should().Be(CardStatus.Rejected);
    }

    [Fact]
    public void Reject_throws_when_the_card_is_not_PendingApproval()
    {
        var card = NewCard();

        var act = () => card.Reject();

        act.Should().Throw<InvalidCardStateException>();
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
        var act = () => NewCard(cardholderName: " ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcd12345678")]
    public void Constructor_rejects_an_invalid_pan(string pan)
    {
        var act = () => NewCard(pan: pan);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcd")]
    public void Constructor_rejects_an_invalid_cvv(string cvv)
    {
        var act = () => NewCard(cvv: cvv);

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

    [Fact]
    public void EnsureCanSpend_throws_CardNotActivated_for_an_unactivated_physical_card()
    {
        var card = NewPhysicalCard();
        card.MarkShipped("UPS", "1Z999", IssuedAt);

        var act = () => card.EnsureCanSpend(new Money(10m, Usd));

        act.Should().Throw<CardNotActivatedException>();
    }

    [Fact]
    public void Constructor_starts_a_virtual_card_as_NotApplicable_fulfillment_and_already_activated()
    {
        var card = NewPendingCard();

        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.NotApplicable);
        card.IsActivated.Should().BeTrue();
    }

    [Fact]
    public void Constructor_starts_a_physical_card_as_Pending_fulfillment_and_not_activated()
    {
        var card = NewPhysicalCard();

        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Pending);
        card.IsActivated.Should().BeFalse();
    }

    [Fact]
    public void MarkShipped_transitions_Pending_to_Shipped_and_records_tracking_details()
    {
        var card = NewPhysicalCard();

        card.MarkShipped("UPS", "1Z999AA10123456784", IssuedAt);

        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Shipped);
        card.ShippingCarrier.Should().Be("UPS");
        card.TrackingNumber.Should().Be("1Z999AA10123456784");
        card.ShippedAt.Should().Be(IssuedAt);
    }

    [Fact]
    public void MarkShipped_throws_when_not_Pending()
    {
        var card = NewPhysicalCard();
        card.MarkShipped("UPS", "1Z999", IssuedAt);

        var act = () => card.MarkShipped("FedEx", "7777", IssuedAt);

        act.Should().Throw<InvalidCardFulfillmentStateException>();
    }

    [Fact]
    public void MarkDelivered_transitions_Shipped_to_Delivered()
    {
        var card = NewPhysicalCard();
        card.MarkShipped("UPS", "1Z999", IssuedAt);

        card.MarkDelivered(IssuedAt);

        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Delivered);
        card.DeliveredAt.Should().Be(IssuedAt);
    }

    [Fact]
    public void MarkDelivered_throws_when_not_Shipped()
    {
        var card = NewPhysicalCard();

        var act = () => card.MarkDelivered(IssuedAt);

        act.Should().Throw<InvalidCardFulfillmentStateException>();
    }

    [Fact]
    public void MarkReturned_is_reachable_from_Pending_or_Shipped()
    {
        var pendingCard = NewPhysicalCard();
        pendingCard.MarkReturned(IssuedAt);
        pendingCard.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Returned);

        var shippedCard = NewPhysicalCard();
        shippedCard.MarkShipped("UPS", "1Z999", IssuedAt);
        shippedCard.MarkReturned(IssuedAt);
        shippedCard.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Returned);
    }

    [Fact]
    public void MarkFailed_throws_once_already_Delivered()
    {
        var card = NewPhysicalCard();
        card.MarkShipped("UPS", "1Z999", IssuedAt);
        card.MarkDelivered(IssuedAt);

        var act = () => card.MarkFailed(IssuedAt);

        act.Should().Throw<InvalidCardFulfillmentStateException>();
    }

    [Fact]
    public void Activate_from_Delivered_sets_IsActivated_without_changing_FulfillmentStatus()
    {
        var card = NewPhysicalCard();
        card.MarkShipped("UPS", "1Z999", IssuedAt);
        card.MarkDelivered(IssuedAt);

        card.Activate(IssuedAt);

        card.IsActivated.Should().BeTrue();
        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Delivered);
    }

    [Fact]
    public void Activate_from_Shipped_also_bumps_FulfillmentStatus_to_Delivered()
    {
        var card = NewPhysicalCard();
        card.MarkShipped("UPS", "1Z999", IssuedAt);

        card.Activate(IssuedAt);

        card.IsActivated.Should().BeTrue();
        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Delivered);
        card.DeliveredAt.Should().Be(IssuedAt);
    }

    [Fact]
    public void Activate_throws_when_still_Pending()
    {
        var card = NewPhysicalCard();

        var act = () => card.Activate(IssuedAt);

        act.Should().Throw<InvalidCardFulfillmentStateException>();
    }

    [Fact]
    public void SetPin_rejects_a_non_four_digit_value()
    {
        var card = NewCard();

        var act = () => card.SetPin("12345");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPin_accepts_a_four_digit_value()
    {
        var card = NewCard();

        card.SetPin("1234");

        card.HasPin.Should().BeTrue();
        card.Pin.Should().Be("1234");
    }
}
