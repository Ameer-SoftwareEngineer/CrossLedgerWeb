namespace CrossLedgerWeb.Domain.Cards;

/// <summary>Plain address data captured on card issuance - display/record-keeping only,
/// nothing in the card's own behaviour reads these fields.</summary>
public sealed record CardAddress(string Street, string Number, string State, string Postcode, string City, string Country);
