namespace CrossLedgerWeb.Domain.Cards;

public enum CardStatus
{
    /// <summary>Every card starts here - an Admin Console review (specification 9's RBAC
    /// proof) must approve it before it can be frozen, spent from, or topped up. Only
    /// reachable from this state, into Active or Rejected.</summary>
    PendingApproval,

    Active,
    Frozen,

    /// <summary>Terminal - a closed card can never reopen.</summary>
    Closed,

    /// <summary>Terminal - an admin declined the request. Distinct from Closed: a closed
    /// card was once usable, a rejected one never was.</summary>
    Rejected,
}
