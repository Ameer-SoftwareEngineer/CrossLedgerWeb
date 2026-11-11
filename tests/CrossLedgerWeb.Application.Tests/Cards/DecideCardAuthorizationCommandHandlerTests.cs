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
    private readonly Mock<IWalletRepository> _wallets = new();
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
        _cards.Object, _authorizations.Object, _purchases.Object, _wallets.Object, _clock.Object,
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

    private void SetUpFundedCard(Card card, decimal balance, Money? openHoldTotal = null)
    {
        var wallet = new Wallet(card.CardWalletId, Owner, card.Currency, WalletKind.Card);
        if (balance > 0)
            wallet.Credit(new Money(balance, card.Currency), TransferId.New(), Now.AddDays(-1));

        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _wallets.Setup(x => x.GetByIdAsync(card.CardWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _authorizations.Setup(x => x.GetOpenHoldTotalAsync(card.Id, card.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(openHoldTotal ?? Money.Zero(card.Currency));
    }

    [Fact]
    public async Task Handle_approves_and_opens_a_hold_without_posting_a_ledger_entry()
    {
        var card = NewCard();
        card.Approve();
        SetUpFundedCard(card, balance: 100m);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None);

        result.Approved.Should().BeTrue();
        result.DeclineReason.Should().BeNull();

        _authorizations.Verify(x => x.Add(It.IsAny<CardAuthorization>()), Times.Once);
        _authorizations.Invocations
            .Where(i => i.Method.Name == nameof(ICardAuthorizationRepository.Add))
            .Select(i => (CardAuthorization)i.Arguments[0])
            .Should().ContainSingle()
            .Which.Should().Match<CardAuthorization>(a =>
                a.Approved
                && a.HoldStatus == CardAuthorizationHoldStatus.Open
                && a.Amount.Amount == 25m
                && a.TransferId == null
                && a.ExpiresAt == Now + TimeSpan.FromDays(7));
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
        SetUpFundedCard(card, balance: 100m);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("card_not_activated");
    }

    [Fact]
    public async Task Handle_declines_as_card_not_active_when_the_card_is_still_pending_approval()
    {
        var card = NewCard(); // never Approved - starts PendingApproval
        SetUpFundedCard(card, balance: 100m);

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
        SetUpFundedCard(card, balance: 100m);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $25.00 > $10 limit

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("spending_limit_exceeded");
    }

    [Fact]
    public async Task Handle_declines_as_insufficient_funds_when_the_wallet_balance_is_too_low()
    {
        var card = NewCard();
        card.Approve();
        SetUpFundedCard(card, balance: 10m);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $25.00 > $10 balance

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("insufficient_funds");
    }

    [Fact]
    public async Task Handle_declines_as_insufficient_funds_when_an_existing_open_hold_already_consumes_available_balance()
    {
        var card = NewCard();
        card.Approve();
        // $100 balance, $80 already held by another open authorization - only $20 available.
        SetUpFundedCard(card, balance: 100m, openHoldTotal: new Money(80m, card.Currency));

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // wants $25.00

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("insufficient_funds");
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
        SetUpFundedCard(card, balance: 100m);

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
        SetUpFundedCard(card, balance: 100m);

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
        SetUpFundedCard(card, balance: 100m);
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
        SetUpFundedCard(card, balance: 100m);
        _purchases.Setup(x => x.GetSpendTotalAsync(card.Id, Now - TimeSpan.FromDays(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(20m);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $20 already + $25 > $40

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("monthly_limit_exceeded");
    }
}
