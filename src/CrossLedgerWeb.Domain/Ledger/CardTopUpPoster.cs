using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;

namespace CrossLedgerWeb.Domain.Ledger;

/// <summary>
/// Posts a top-up as two balanced ledger entries: debit the funding wallet, credit the
/// card's own wallet. Two legs, same currency on both sides - no settlement account
/// involved, unlike CardPurchasePoster/PayoutReserver, because the money never leaves the
/// owner's own wallets; it only moves from one of their wallets into the dedicated one
/// backing their card.
/// </summary>
public static class CardTopUpPoster
{
    public static IReadOnlyList<LedgerEntry> Post(
        TransferId transferId,
        Wallet sourceWallet,
        Wallet cardWallet,
        Money amount,
        DateTimeOffset postedAt)
    {
        var entries = new[]
        {
            sourceWallet.Debit(amount, transferId, postedAt),
            cardWallet.Credit(amount, transferId, postedAt),
        };

        LedgerInvariants.AssertZeroSumPerCurrency(entries);

        return entries;
    }
}
