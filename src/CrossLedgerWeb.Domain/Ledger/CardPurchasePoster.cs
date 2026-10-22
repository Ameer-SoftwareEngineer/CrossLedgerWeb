using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;

namespace CrossLedgerWeb.Domain.Ledger;

/// <summary>
/// Posts one simulated card purchase as two balanced ledger entries: debit the
/// cardholder's wallet, credit the MerchantSettlement wallet for that currency. Two legs,
/// not TransferPoster's four - same reasoning as PayoutReserver: the money is leaving the
/// platform to a merchant, not moving between two of the platform's own customer wallets,
/// so there is no second currency leg to post.
/// </summary>
public static class CardPurchasePoster
{
    public static IReadOnlyList<LedgerEntry> Post(
        TransferId transferId,
        Wallet cardholderWallet,
        Wallet merchantSettlementWallet,
        Money amount,
        DateTimeOffset postedAt)
    {
        var entries = new[]
        {
            cardholderWallet.Debit(amount, transferId, postedAt),
            merchantSettlementWallet.Credit(amount, transferId, postedAt),
        };

        LedgerInvariants.AssertZeroSumPerCurrency(entries);

        return entries;
    }
}
