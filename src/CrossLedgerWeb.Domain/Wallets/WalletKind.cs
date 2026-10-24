namespace CrossLedgerWeb.Domain.Wallets;

public enum WalletKind
{
    /// <summary>A customer-owned wallet. Must never be debited below zero.</summary>
    Customer,

    /// <summary>An internal FX settlement / clearing account. Legitimately runs a
    /// negative balance between the two legs of a cross-currency transfer, so it is
    /// exempt from the non-negative balance check.</summary>
    SystemClearing,

    /// <summary>Holds funds debited from a customer wallet for a payout that is in
    /// flight to an external provider (specification 5.5's RESERVED state: "funds held,
    /// ledger entries written"). Distinct from <see cref="SystemClearing"/> - that account
    /// is for the two legs of an internal, same-platform FX transfer; this one is for
    /// money that is actually leaving the platform. Exempt from the non-negative check for
    /// the same reason SystemClearing is.</summary>
    PayoutReserve,

    /// <summary>Holds funds debited from a customer wallet for a simulated card
    /// purchase. Same reasoning as <see cref="PayoutReserve"/> - the money is leaving the
    /// platform to a merchant, not moving between two of the platform's own wallets - kept
    /// as its own kind rather than reusing PayoutReserve so the two settlement purposes
    /// stay distinguishable in the ledger. Exempt from the non-negative check for the same
    /// reason the other two are.</summary>
    MerchantSettlement,

    /// <summary>The dedicated wallet backing one Card (prepaid model) - starts at zero,
    /// only ever credited by a TopUp and debited by a simulated purchase. Unlike the
    /// other non-Customer kinds, this one IS subject to the non-negative balance check
    /// (a prepaid card cannot overdraw) - kept as its own kind rather than reusing
    /// Customer so it can be filtered out of the regular Wallets list/sidebar, which
    /// should only ever show wallets the owner manages directly.</summary>
    Card,
}
