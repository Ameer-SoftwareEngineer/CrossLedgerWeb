namespace CrossLedgerWeb.Application.Cards;

/// <summary>StripeCardholderId/StripeCardId are null for a SimulatedCardIssuer result -
/// there is no external resource to reference, same reasoning as Payout.ProviderCode
/// staying null until a real provider is actually involved.</summary>
public sealed record IssuedCardDetails(
    string Pan, string Cvv, int ExpiryMonth, int ExpiryYear, string? StripeCardholderId, string? StripeCardId);
