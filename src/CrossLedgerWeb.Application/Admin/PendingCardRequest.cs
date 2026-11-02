using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Admin;

public sealed record PendingCardRequest(
    CardId Id,
    UserId OwnerId,
    string OwnerEmail,
    string Currency,
    CardType CardType,
    string CardholderName,
    SourceOfFunds SourceOfFunds,
    decimal EstimatedGrossAnnualIncome,
    decimal EstimatedNetWorth,
    bool IsActivelyEmployed,
    string? EmployerCompanyName,
    string? JobTitle,
    CardAddress RegisteredAddress,
    CardAddress ShippingAddress,
    DateTimeOffset RequestedAt);
