namespace CrossLedgerWeb.Domain.Cards;

/// <summary>
/// The lifecycle of the funds a real-time authorization decision holds (specification 12.3/
/// 12.4's "Settlement" week) - layered entirely on top of <see cref="CardAuthorization"/>,
/// never inside the ledger itself. A hold never changes <see cref="Wallets.Wallet.Balance"/>;
/// it only changes what counts as *available* to spend, by being summed and subtracted from
/// the wallet's real balance wherever that check happens.
/// </summary>
public enum CardAuthorizationHoldStatus
{
    /// <summary>A declined authorization never held anything.</summary>
    NotApplicable,

    /// <summary>Approved; funds are held (counted against available balance) but nothing has
    /// been posted to the ledger yet. Resolves to either Captured or Released.</summary>
    Open,

    /// <summary>A real purchase was posted for the captured amount (which may be less than
    /// the original hold - a partial capture) - terminal.</summary>
    Captured,

    /// <summary>The hold expired (or was otherwise released) without ever being captured - no
    /// ledger entry was ever created for it, so releasing it is just a status flip - terminal.</summary>
    Released,

    /// <summary>A capture webhook arrived for more than the wallet could cover (an
    /// over-capture, specification 12.3's "partial and incremental captures" taken to its
    /// unhappy extreme) - unlike Released, the merchant's side DID actually capture the
    /// money; CrossLedgerWeb just couldn't mirror it internally. Same spirit as the spec's
    /// own PENDING_MANUAL state for a payout every provider rejects: funds stay reserved,
    /// nothing is silently lost, and an admin has to look at it. Resolves to
    /// ManuallyResolved.</summary>
    PendingManualReview,

    /// <summary>An admin acknowledged a PendingManualReview row - a deliberately simple
    /// acknowledgment, not an automatic money movement. Deciding how to collect or write
    /// off the shortfall is a business-policy call this terminal state does not make for
    /// you.</summary>
    ManuallyResolved,
}
