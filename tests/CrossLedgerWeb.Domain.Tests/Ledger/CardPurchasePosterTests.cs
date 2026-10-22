using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;

namespace CrossLedgerWeb.Domain.Tests.Ledger;

public class CardPurchasePosterTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Wallet CustomerWallet(decimal openingBalance = 0m)
    {
        var wallet = new Wallet(WalletId.New(), UserId.New(), Usd);
        if (openingBalance > 0)
            wallet.Credit(new Money(openingBalance, Usd), TransferId.New(), Now);
        return wallet;
    }

    private static Wallet MerchantSettlementWallet() =>
        new(WalletId.New(), UserId.New(), Usd, WalletKind.MerchantSettlement);

    [Fact]
    public void Posting_a_purchase_produces_two_balanced_entries()
    {
        var cardholderWallet = CustomerWallet(openingBalance: 100m);
        var merchantWallet = MerchantSettlementWallet();
        var transferId = TransferId.New();

        var entries = CardPurchasePoster.Post(transferId, cardholderWallet, merchantWallet, new Money(40m, Usd), Now);

        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(e => e.TransferId == transferId);

        cardholderWallet.Balance.Amount.Should().Be(60m);
        merchantWallet.Balance.Amount.Should().Be(40m);

        LedgerInvariants.AssertZeroSumPerCurrency(entries);
    }

    [Fact]
    public void Insufficient_funds_throws_and_posts_nothing_to_either_wallet()
    {
        var cardholderWallet = CustomerWallet(openingBalance: 10m);
        var merchantWallet = MerchantSettlementWallet();

        var act = () => CardPurchasePoster.Post(TransferId.New(), cardholderWallet, merchantWallet, new Money(40m, Usd), Now);

        act.Should().Throw<InsufficientFundsException>();
        cardholderWallet.Entries.Should().HaveCount(1); // only the opening credit from test setup
        merchantWallet.Entries.Should().BeEmpty();
    }
}
