namespace CrossLedgerWeb.Shared.Auth;

public sealed record MyProfileResponse(
    Guid UserId,
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
    string RegistrationStatus);
