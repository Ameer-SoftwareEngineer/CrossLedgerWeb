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
}
