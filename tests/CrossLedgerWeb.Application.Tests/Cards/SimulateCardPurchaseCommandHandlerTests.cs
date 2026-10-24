using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class SimulateCardPurchaseCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardPurchaseRepository> _purchases = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IMerchantSettlementWalletResolver> _merchantSettlementWallets = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Card _card;
    private readonly Wallet _cardWallet;
    private readonly Wallet _merchantWallet;

    public SimulateCardPurchaseCommandHandlerTests()
    {
        _cardWallet = new Wallet(WalletId.New(), Owner, Usd, WalletKind.Card);
        _cardWallet.Credit(new Money(500m, Usd), TransferId.New(), Now);
        _merchantWallet = new Wallet(WalletId.New(), UserId.New(), Usd, WalletKind.MerchantSettlement);

        _card = new Card(
            CardId.New(), Owner, _cardWallet.Id, Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);

        _cards.Setup(x => x.GetByIdAsync(_card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_card);
        _wallets.Setup(x => x.GetByIdAsync(_cardWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_cardWallet);
        _merchantSettlementWallets.Setup(x => x.GetMerchantSettlementWalletAsync(Usd, It.IsAny<CancellationToken>())).ReturnsAsync(_merchantWallet);
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private SimulateCardPurchaseCommandHandler CreateHandler() => new(
        _cards.Object, _purchases.Object, _wallets.Object, _merchantSettlementWallets.Object, _clock.Object);

    private SimulateCardPurchaseCommand ValidCommand(decimal amount = 40m) =>
        new(_card.Id, Owner, "Coffee Shop", amount, "idem-key-1");

    [Fact]
    public async Task Handle_debits_the_card_wallet_and_credits_the_merchant_settlement_wallet()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(40m), CancellationToken.None);

        result.Amount.Should().Be(new Money(40m, Usd));
        _cardWallet.Balance.Amount.Should().Be(460m);
        _merchantWallet.Balance.Amount.Should().Be(40m);
    }

    [Fact]
    public async Task Handle_stages_the_purchase_record_for_insertion()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        _purchases.Verify(x => x.Add(It.Is<CardPurchase>(p => p.Id == result.PurchaseId && p.MerchantName == "Coffee Shop")), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_does_not_own_the_card()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand() with { CallerId = UserId.New() }, CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_card_is_frozen()
    {
        _card.Freeze();
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCardStateException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_amount_exceeds_the_spending_limit()
    {
        _card.SetSpendingLimit(20m);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(40m), CancellationToken.None);

        await act.Should().ThrowAsync<CardSpendingLimitExceededException>();
    }

    [Fact]
    public async Task Handle_propagates_insufficient_funds_from_the_domain()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(1_000m), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }
}
