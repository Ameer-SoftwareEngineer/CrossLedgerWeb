namespace CrossLedgerWeb.Application.Cards;

/// <summary>The sensitive pair a step-up-gated reveal returns - everything else about a
/// card is already visible from CardSummary.</summary>
public sealed record RevealCardDetailsResult(string Pan, string Cvv, int ExpiryMonth, int ExpiryYear);
