namespace CrossLedgerWeb.Domain.Cards;

public enum CardStatus
{
    Active,
    Frozen,

    /// <summary>Terminal - a closed card can never reopen.</summary>
    Closed,
}
