using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string? MiddleName,
    string LastName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    string Address,
    string PermanentAddress,
    string City,
    string StateProvince,
    string Country,
    string ProofOfAddressDocumentType,
    string ProofOfAddressFileName,
    string ProofOfAddressContentType,
    byte[] ProofOfAddressContent) : IRequest<RegisterResult>;

public sealed record RegisterResult(UserId UserId, string Email);
