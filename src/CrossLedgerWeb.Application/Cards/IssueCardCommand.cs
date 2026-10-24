using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Issues a new prepaid card: a dedicated, zero-balance Card-kind wallet is
/// created alongside it. ShippingAddress null means "same as registered" - the handler
/// copies RegisteredAddress rather than leaving it unset, so the card always has a
/// concrete shipping address on record.</summary>
public sealed record IssueCardCommand(
    UserId OwnerId,
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
    CardAddress? ShippingAddress) : IRequest<IssueCardResult>;
