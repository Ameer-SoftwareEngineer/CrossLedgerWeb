using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class DecideCardAuthorizationCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");
    private const string StripeCardId = "ic_test123";
    private const string StripeAuthorizationId = "iauth_test456";

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardAuthorizationRepository> _authorizations = new();
    private readonly Mock<ICardPurchaseRepository> _purchases = new();
    private readonly Mock<ICardAuthorizationHoldReservation> _holdReservation = new();
    private readonly Mock<IClock> _clock = new();
    private readonly CardIssuingOptions _options = new() { AuthorizationHoldDays = 7 };

    public DecideCardAuthorizationCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _authorizations.Setup(x => x.GetByStripeAuthorizationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardAuthorization?)null);
        _purchases.Setup(x => x.GetSpendTotalAsync(It.IsAny<CardId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
    }

    private DecideCardAuthorizationCommandHandler CreateHandler() => new(
        _cards.Object, _authorizations.Object, _purchases.Object, _holdReservation.Object, _clock.Object,
        Options.Create(_options), NullLogger<DecideCardAuthorizationCommandHandler>.Instance);

    private static Card NewCard()
    {
        var card = new Card(
            CardId.New(), Owner, WalletId.New(), Currency.From("USD"), CardType.Virtual, "Test Customer",
            "4532750123456784", "123", 1, Now.Year + 3, Now,
            SourceOfFunds.Salary, 50000m, 100000m, true, "Acme Inc", "Developer",
            TestAddress, TestAddress, stripeCardholderId: "ich_test", stripeCardId: StripeCardId);
        return card;
    }

    private static DecideCardAuthorizationCommand Command(
        long amountMinorUnits = 2500, string currency = "usd", string? merchantCategoryCode = null, string? merchantCountry = null) =>
        new(StripeAuthorizationId, StripeCardId, amountMinorUnits, currency, "Stripe Coffee Co", merchantCategoryCode, merchantCountry);

    private void SetUpCard(Card card)
    {
        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);
    }

    private void SetUpReservation(bool approved, string? declineReason) =>
        _holdReservation
            .Setup(x => x.ReserveAsync(
                It.IsAny<CardAuthorizationId>(), It.IsAny<CardId>(), It.IsAny<WalletId>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Money>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CardAuthorizationHoldReservationResult(approved, declineReason));

    [Fact]
    public async Task Handle_approves_by_delegating_the_balance_decision_to_the_hold_reservation()
    {
        var card = NewCard();
        card.Approve();
        SetUpCard(card);
        SetUpReservation(approved: true, declineReason: null);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None);

        result.Approved.Should().BeTrue();
        result.DeclineReason.Should().BeNull();

        // The reservation proc makes the decision AND inserts the audit row atomically -
        // the handler must not also insert one via EF for this path.
        _authorizations.Verify(x => x.Add(It.IsAny<CardAuthorization>()), Times.Never);
        _holdReservation.Verify(x => x.ReserveAsync(
            It.IsAny<CardAuthorizationId>(), card.Id, card.CardWalletId, StripeAuthorizationId, "Stripe Coffee Co",
            It.Is<Money>(m => m.Amount == 25m), Now, Now + TimeSpan.FromDays(7), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_declines_as_card_not_activated_for_an_unactivated_physical_card()
    {
        var card = new Card(
            CardId.New(), Owner, WalletId.New(), Currency.From("USD"), CardType.Physical, "Test Customer",
            "4532750123456784", "123", 1, Now.Year + 3, Now,
            SourceOfFunds.Salary, 50000m, 100000m, true, "Acme Inc", "Developer",
            TestAddress, TestAddress, stripeCardholderId: "ich_test", stripeCardId: StripeCardId);
        card.Approve();
        card.MarkShipped("UPS", "1Z999", Now);
        SetUpCard(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("card_not_activated");
        _holdReservation.Verify(x => x.ReserveAsync(
            It.IsAny<CardAuthorizationId>(), It.IsAny<CardId>(), It.IsAny<WalletId>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Money>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_declines_as_card_not_active_when_the_card_is_still_pending_approval()
    {
        var card = NewCard(); // never Approved - starts PendingApproval
        SetUpCard(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("card_not_active");
    }

    [Fact]
    public async Task Handle_declines_as_spending_limit_exceeded_when_over_the_cards_own_limit()
    {
        var card = NewCard();
        card.Approve();
        card.SetSpendingLimit(10m);
        SetUpCard(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $25.00 > $10 limit

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("spending_limit_exceeded");
    }

    [Fact]
    public async Task Handle_declines_as_insufficient_funds_when_the_hold_reservation_reports_no_available_balance()
    {
        var card = NewCard();
        card.Approve();
        SetUpCard(card);
        SetUpReservation(approved: false, declineReason: "insufficient_funds");

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("insufficient_funds");
        // The reservation proc already inserted the declined audit row - no second EF insert.
        _authorizations.Verify(x => x.Add(It.IsAny<CardAuthorization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_declines_as_currency_mismatch_without_an_audit_record_when_currencies_differ()
    {
        var card = NewCard(); // USD
        card.Approve();
        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(currency: "eur"), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("currency_mismatch");
        _authorizations.Verify(x => x.Add(It.IsAny<CardAuthorization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_declines_as_unknown_card_without_an_audit_record_when_the_stripe_card_id_is_not_ours()
    {
        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync((Card?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("unknown_card");
        _authorizations.Verify(x => x.Add(It.IsAny<CardAuthorization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_the_original_decision_without_reopening_a_hold_when_the_authorization_id_was_already_decided()
    {
        var existing = new CardAuthorization(
            CardAuthorizationId.New(), CardId.New(), StripeAuthorizationId, new Money(25m, Currency.From("USD")),
            "Stripe Coffee Co", approved: false, declineReason: "insufficient_funds", holdDuration: null, decidedAt: Now);
        _authorizations.Setup(x => x.GetByStripeAuthorizationIdAsync(StripeAuthorizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("insufficient_funds");
        _cards.Verify(x => x.GetByStripeCardIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _authorizations.Verify(x => x.Add(It.IsAny<CardAuthorization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_declines_as_card_category_blocked_when_the_merchant_category_is_on_the_block_list()
    {
        var card = NewCard();
        card.Approve();
        card.SetSpendingControls(new HashSet<string> { "7995" }, new HashSet<string>(), null, null);
        SetUpCard(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(merchantCategoryCode: "7995"), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("card_category_blocked");
    }

    [Fact]
    public async Task Handle_declines_as_country_not_allowed_when_the_merchant_country_is_not_on_the_allow_list()
    {
        var card = NewCard();
        card.Approve();
        card.SetSpendingControls(new HashSet<string>(), new HashSet<string> { "US" }, null, null);
        SetUpCard(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(merchantCountry: "FR"), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("country_not_allowed");
    }

    [Fact]
    public async Task Handle_declines_as_daily_limit_exceeded_when_todays_spend_plus_this_amount_is_over_the_cap()
    {
        var card = NewCard();
        card.Approve();
        card.SetSpendingControls(new HashSet<string>(), new HashSet<string>(), dailyLimit: 30m, monthlyLimit: null);
        SetUpCard(card);
        _purchases.Setup(x => x.GetSpendTotalAsync(card.Id, Now - TimeSpan.FromHours(24), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10m);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $10 already + $25 > $30

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("daily_limit_exceeded");
    }

    [Fact]
    public async Task Handle_declines_as_monthly_limit_exceeded_when_this_months_spend_plus_this_amount_is_over_the_cap()
    {
        var card = NewCard();
        card.Approve();
        card.SetSpendingControls(new HashSet<string>(), new HashSet<string>(), dailyLimit: null, monthlyLimit: 40m);
        SetUpCard(card);
        _purchases.Setup(x => x.GetSpendTotalAsync(card.Id, Now - TimeSpan.FromDays(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(20m);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $20 already + $25 > $40

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("monthly_limit_exceeded");
    }
}
