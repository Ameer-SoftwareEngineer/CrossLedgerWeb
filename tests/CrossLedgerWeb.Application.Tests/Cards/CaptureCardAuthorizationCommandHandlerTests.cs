using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class CaptureCardAuthorizationCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");
    private const string StripeCardId = "ic_test123";
    private const string StripeAuthorizationId = "iauth_test456";
    private const string StripeTransactionId = "ipi_test789";

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardAuthorizationRepository> _authorizations = new();
    private readonly Mock<ICardPurchaseRepository> _purchases = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IMerchantSettlementWalletResolver> _merchantSettlementWallets = new();
    private readonly Mock<IClock> _clock = new();

    public CaptureCardAuthorizationCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _authorizations.Setup(x => x.GetByStripeTransactionIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardAuthorization?)null);
    }

    private CaptureCardAuthorizationCommandHandler CreateHandler() => new(
        _cards.Object, _authorizations.Object, _purchases.Object, _wallets.Object, _merchantSettlementWallets.Object,
        _clock.Object, NullLogger<CaptureCardAuthorizationCommandHandler>.Instance);

    private static Card NewApprovedCard()
    {
        var card = new Card(
            CardId.New(), Owner, WalletId.New(), Currency.From("USD"), CardType.Virtual, "Test Customer",
            "4532750123456784", "123", 1, Now.Year + 3, Now,
            SourceOfFunds.Salary, 50000m, 100000m, true, "Acme Inc", "Developer",
            TestAddress, TestAddress, stripeCardholderId: "ich_test", stripeCardId: StripeCardId);
        card.Approve();
        return card;
    }

    private static CardAuthorization NewOpenHold(CardId cardId, decimal heldAmount) =>
        new(CardAuthorizationId.New(), cardId, StripeAuthorizationId, new Money(heldAmount, Currency.From("USD")),
            "Stripe Coffee Co", approved: true, declineReason: null, holdDuration: TimeSpan.FromDays(7), decidedAt: Now.AddMinutes(-5));

    private void SetUp(Card card, CardAuthorization hold, Wallet wallet, Wallet merchantWallet)
    {
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _authorizations.Setup(x => x.GetByStripeAuthorizationIdAsync(StripeAuthorizationId, It.IsAny<CancellationToken>())).ReturnsAsync(hold);
        _wallets.Setup(x => x.GetByIdAsync(card.CardWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _merchantSettlementWallets.Setup(x => x.GetMerchantSettlementWalletAsync(card.Currency, It.IsAny<CancellationToken>())).ReturnsAsync(merchantWallet);
    }

    private static CaptureCardAuthorizationCommand Command(long capturedMinorUnits) =>
        new(StripeAuthorizationId, StripeTransactionId, capturedMinorUnits, "usd");

    [Fact]
    public async Task Handle_posts_the_full_captured_amount_and_closes_the_hold_on_a_full_capture()
    {
        var card = NewApprovedCard();
        var hold = NewOpenHold(card.Id, heldAmount: 25m);
        var wallet = new Wallet(card.CardWalletId, Owner, card.Currency, WalletKind.Card);
        wallet.Credit(new Money(100m, card.Currency), TransferId.New(), Now.AddDays(-1));
        var merchantWallet = new Wallet(WalletId.New(), UserId.New(), card.Currency, WalletKind.MerchantSettlement);
        SetUp(card, hold, wallet, merchantWallet);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(capturedMinorUnits: 2500), CancellationToken.None); // full $25.00

        result.Captured.Should().BeTrue();
        wallet.Balance.Amount.Should().Be(75m);
        hold.HoldStatus.Should().Be(CardAuthorizationHoldStatus.Captured);
        hold.CapturedAmount.Should().Be(25m);
        hold.TransferId.Should().NotBeNull();
        hold.StripeTransactionId.Should().Be(StripeTransactionId);
        _purchases.Verify(x => x.Add(It.Is<CardPurchase>(p => p.Amount.Amount == 25m)), Times.Once);
    }

    [Fact]
    public async Task Handle_posts_only_the_partial_amount_and_closes_the_hold_on_a_partial_capture()
    {
        var card = NewApprovedCard();
        var hold = NewOpenHold(card.Id, heldAmount: 25m);
        var wallet = new Wallet(card.CardWalletId, Owner, card.Currency, WalletKind.Card);
        wallet.Credit(new Money(100m, card.Currency), TransferId.New(), Now.AddDays(-1));
        var merchantWallet = new Wallet(WalletId.New(), UserId.New(), card.Currency, WalletKind.MerchantSettlement);
        SetUp(card, hold, wallet, merchantWallet);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(capturedMinorUnits: 1500), CancellationToken.None); // only $15.00 of the $25 held

        result.Captured.Should().BeTrue();
        wallet.Balance.Amount.Should().Be(85m); // only the captured $15 ever left the wallet
        hold.HoldStatus.Should().Be(CardAuthorizationHoldStatus.Captured);
        hold.CapturedAmount.Should().Be(15m);
        _purchases.Verify(x => x.Add(It.Is<CardPurchase>(p => p.Amount.Amount == 15m)), Times.Once);
    }

    [Fact]
    public async Task Handle_is_idempotent_on_a_replayed_stripe_transaction_id()
    {
        var card = NewApprovedCard();
        var hold = NewOpenHold(card.Id, heldAmount: 25m);
        hold.Capture(StripeTransactionId, new Money(25m, card.Currency), TransferId.New(), Now);
        _authorizations.Setup(x => x.GetByStripeTransactionIdAsync(StripeTransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(hold);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(capturedMinorUnits: 2500), CancellationToken.None);

        result.Captured.Should().BeTrue();
        _cards.Verify(x => x.GetByIdAsync(It.IsAny<CardId>(), It.IsAny<CancellationToken>()), Times.Never);
        _purchases.Verify(x => x.Add(It.IsAny<CardPurchase>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ignores_a_capture_for_an_unknown_authorization()
    {
        _authorizations.Setup(x => x.GetByStripeAuthorizationIdAsync(StripeAuthorizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardAuthorization?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(capturedMinorUnits: 2500), CancellationToken.None);

        result.Captured.Should().BeFalse();
        result.Reason.Should().Be("unknown_authorization");
    }

    [Fact]
    public async Task Handle_ignores_a_capture_against_a_hold_that_is_already_resolved()
    {
        var card = NewApprovedCard();
        var hold = NewOpenHold(card.Id, heldAmount: 25m);
        hold.Release(Now); // already released by the expiry sweeper
        _authorizations.Setup(x => x.GetByStripeAuthorizationIdAsync(StripeAuthorizationId, It.IsAny<CancellationToken>())).ReturnsAsync(hold);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(capturedMinorUnits: 2500), CancellationToken.None);

        result.Captured.Should().BeFalse();
        result.Reason.Should().Be("hold_not_open");
        _purchases.Verify(x => x.Add(It.IsAny<CardPurchase>()), Times.Never);
    }

    [Fact]
    public async Task Handle_leaves_the_hold_open_and_reports_insufficient_funds_on_an_overcapture_the_wallet_cannot_cover()
    {
        var card = NewApprovedCard();
        var hold = NewOpenHold(card.Id, heldAmount: 25m);
        var wallet = new Wallet(card.CardWalletId, Owner, card.Currency, WalletKind.Card);
        wallet.Credit(new Money(25m, card.Currency), TransferId.New(), Now.AddDays(-1)); // only enough for the hold itself
        var merchantWallet = new Wallet(WalletId.New(), UserId.New(), card.Currency, WalletKind.MerchantSettlement);
        SetUp(card, hold, wallet, merchantWallet);

        var handler = CreateHandler();
        var result = await handler.Handle(Command(capturedMinorUnits: 5000), CancellationToken.None); // $50.00 over-capture

        result.Captured.Should().BeFalse();
        result.Reason.Should().Be("insufficient_funds");
        hold.HoldStatus.Should().Be(CardAuthorizationHoldStatus.Open);
        wallet.Balance.Amount.Should().Be(25m);
    }
}
