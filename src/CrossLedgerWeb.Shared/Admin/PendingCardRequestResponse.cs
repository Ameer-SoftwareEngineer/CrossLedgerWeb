using CrossLedgerWeb.Shared.Cards;

namespace CrossLedgerWeb.Shared.Admin;

public sealed record PendingCardRequestResponse(
    Guid CardId,
    Guid OwnerId,
    string OwnerEmail,
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
    CardAddressDto ShippingAddress,
    DateTimeOffset RequestedAt);
