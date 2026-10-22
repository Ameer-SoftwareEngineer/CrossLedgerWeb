namespace CrossLedgerWeb.Shared.Cards;

public sealed record RevealCardDetailsResponse(string Pan, string Cvv, int ExpiryMonth, int ExpiryYear);
