namespace CrossLedgerWeb.Application.Auth;

/// <summary>Everything IdentityService needs to create both the login credentials and the
/// KYC profile in one CreateAsync call - kept as plain values (not the Api layer's
/// IFormFile) so Application never depends on ASP.NET Core's HTTP model binding types.</summary>
public sealed record RegistrationDetails(
    string Email,
    string Password,
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
    string ProofOfAddressContentType,
    byte[] ProofOfAddressContent);
