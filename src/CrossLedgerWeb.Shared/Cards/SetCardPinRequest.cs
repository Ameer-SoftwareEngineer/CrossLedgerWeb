namespace CrossLedgerWeb.Shared.Cards;

/// <summary>Server re-validates the 4-digit shape regardless of client-side validation.</summary>
public sealed record SetCardPinRequest(string Pin);
