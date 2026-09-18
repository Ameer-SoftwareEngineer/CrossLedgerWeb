using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Auth;

public sealed record MyProfile(
    UserId UserId,
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
    RegistrationStatus RegistrationStatus);
