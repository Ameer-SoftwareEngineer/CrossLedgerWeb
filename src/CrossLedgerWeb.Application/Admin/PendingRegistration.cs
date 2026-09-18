using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Admin;

public sealed record PendingRegistration(
    UserId Id,
    string Email,
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
    DateTimeOffset SubmittedAt);
