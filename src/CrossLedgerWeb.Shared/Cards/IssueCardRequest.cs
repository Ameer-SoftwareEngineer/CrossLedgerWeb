namespace CrossLedgerWeb.Shared.Cards;

/// <summary>ShippingAddress null means "same as registered".</summary>
public sealed record IssueCardRequest(
    string Currency,
    string CardType,
    string CardholderName,
    string SourceOfFunds,
    decimal EstimatedGrossAnnualIncome,
    decimal EstimatedNetWorth,
    bool IsActivelyEmployed,
    string? EmployerCompanyName,
    string? JobTitle,
    CardAddressDto RegisteredAddress,
    CardAddressDto? ShippingAddress);
