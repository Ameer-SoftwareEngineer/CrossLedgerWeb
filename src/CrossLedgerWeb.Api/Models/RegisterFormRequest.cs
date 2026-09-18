namespace CrossLedgerWeb.Api.Models;

/// <summary>Registration now carries a PDF upload alongside the KYC text fields, so the
/// wire format is multipart/form-data rather than JSON - this binds via [FromForm] and
/// stays Api-only; Shared (referenced by the Blazor client) never needs IFormFile since
/// the client builds its own MultipartFormDataContent by hand.</summary>
public sealed class RegisterFormRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PermanentAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ProofOfAddressDocumentType { get; set; } = string.Empty;
    public IFormFile ProofOfAddress { get; set; } = null!;
}
