namespace CrossLedgerWeb.Shared.Admin;

public sealed record PendingRegistrationResponse(
    Guid Id,
    string Email,
    string FullName,
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
