using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly Mock<IMerchantSettlementWalletResolver> _merchantSettlementWallets = new();
    private readonly Mock<IClock> _clock = new();

    public DecideCardAuthorizationCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _authorizations.Setup(x => x.GetByStripeAuthorizationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardAuthorization?)null);
    }

    private DecideCardAuthorizationCommandHandler CreateHandler() => new(
        _cards.Object, _authorizations.Object, _purchases.Object, _wallets.Object, _merchantSettlementWallets.Object,
        _clock.Object, NullLogger<DecideCardAuthorizationCommandHandler>.Instance);

    private static Card NewCard()
    {
        var card = new Card(
            CardId.New(), Owner, WalletId.New(), Currency.From("USD"), CardType.Virtual, "Test Customer",
            "4532750123456784", "123", 1, Now.Year + 3, Now,
            SourceOfFunds.Salary, 50000m, 100000m, true, "Acme Inc", "Developer",
            TestAddress, TestAddress, stripeCardholderId: "ich_test", stripeCardId: StripeCardId);
        return card;
    }

    private static DecideCardAuthorizationCommand Command(long amountMinorUnits = 2500, string currency = "usd") =>
        new(StripeAuthorizationId, StripeCardId, amountMinorUnits, currency, "Stripe Coffee Co");

    [Fact]
    public async Task Handle_approves_and_posts_a_purchase_when_the_card_is_active_and_funded()
    {
        var card = NewCard();
        card.Approve();
        var wallet = new Wallet(card.CardWalletId, Owner, card.Currency, WalletKind.Card);
        wallet.Credit(new Money(100m, card.Currency), TransferId.New(), Now.AddDays(-1));
        var merchantWallet = new Wallet(WalletId.New(), UserId.New(), card.Currency, WalletKind.MerchantSettlement);

        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _wallets.Setup(x => x.GetByIdAsync(card.CardWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _merchantSettlementWallets.Setup(x => x.GetMerchantSettlementWalletAsync(card.Currency, It.IsAny<CancellationToken>())).ReturnsAsync(merchantWallet);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None);

        result.Approved.Should().BeTrue();
        result.DeclineReason.Should().BeNull();
        wallet.Balance.Amount.Should().Be(75m);
        merchantWallet.Balance.Amount.Should().Be(25m);
        _purchases.Verify(x => x.Add(It.Is<CardPurchase>(p => p.CardId == card.Id && p.Amount.Amount == 25m && p.MerchantName == "Stripe Coffee Co")), Times.Once);
        _authorizations.Verify(x => x.Add(It.Is<CardAuthorization>(a => a.Approved && a.TransferId != null && a.DeclineReason == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_declines_as_card_not_active_when_the_card_is_still_pending_approval()
    {
        var card = NewCard(); // never Approved - starts PendingApproval
        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("card_not_active");
        _purchases.Verify(x => x.Add(It.IsAny<CardPurchase>()), Times.Never);
        _authorizations.Verify(x => x.Add(It.Is<CardAuthorization>(a => !a.Approved && a.DeclineReason == "card_not_active")), Times.Once);
    }

    [Fact]
    public async Task Handle_declines_as_card_not_active_when_the_card_is_frozen()
    {
        var card = NewCard();
        card.Approve();
        card.Freeze();
        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);

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
        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $25.00 > $10 limit

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("spending_limit_exceeded");
        _wallets.Verify(x => x.GetByIdAsync(It.IsAny<WalletId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_declines_as_insufficient_funds_when_the_wallet_balance_is_too_low()
    {
        var card = NewCard();
        card.Approve();
        var wallet = new Wallet(card.CardWalletId, Owner, card.Currency, WalletKind.Card);
        wallet.Credit(new Money(10m, card.Currency), TransferId.New(), Now.AddDays(-1));
        var merchantWallet = new Wallet(WalletId.New(), UserId.New(), card.Currency, WalletKind.MerchantSettlement);

        _cards.Setup(x => x.GetByStripeCardIdAsync(StripeCardId, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _wallets.Setup(x => x.GetByIdAsync(card.CardWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _merchantSettlementWallets.Setup(x => x.GetMerchantSettlementWalletAsync(card.Currency, It.IsAny<CancellationToken>())).ReturnsAsync(merchantWallet);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(amountMinorUnits: 2500), CancellationToken.None); // $25.00 > $10 balance

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("insufficient_funds");
        wallet.Balance.Amount.Should().Be(10m);
        _purchases.Verify(x => x.Add(It.IsAny<CardPurchase>()), Times.Never);
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
    public async Task Handle_returns_the_original_decision_without_reposting_when_the_authorization_id_was_already_decided()
    {
        var card = NewCard();
        card.Approve();
        var existing = new CardAuthorization(
            CardAuthorizationId.New(), card.Id, StripeAuthorizationId, new Money(25m, card.Currency),
            "Stripe Coffee Co", approved: false, declineReason: "insufficient_funds", transferId: null, decidedAt: Now);
        _authorizations.Setup(x => x.GetByStripeAuthorizationIdAsync(StripeAuthorizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("insufficient_funds");
        _cards.Verify(x => x.GetByStripeCardIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _authorizations.Verify(x => x.Add(It.IsAny<CardAuthorization>()), Times.Never);
    }
}
