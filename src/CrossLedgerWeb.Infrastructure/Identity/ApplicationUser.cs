using CrossLedgerWeb.Domain.Auth;
using Microsoft.AspNetCore.Identity;

namespace CrossLedgerWeb.Infrastructure.Identity;

/// <summary>ASP.NET Core Identity's user record. Deliberately not the same type as
/// Domain's UserId/User concept - Identity's IdentityUser carries framework-specific
/// concerns (password hash, lockout counters, security stamp) that Domain has no
/// business knowing about. Its Id doubles as the UserId Guid used everywhere else.
///
/// Also carries the KYC profile captured at registration (specification 9's Admin
/// Console approval gate) - there's exactly one proof-of-address document per account and
/// no history of edits, so it lives on this row rather than a separate aggregate.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PermanentAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.Pending;
    public DateTimeOffset RegistrationSubmittedAt { get; set; }

    public string ProofOfAddressDocumentType { get; set; } = string.Empty;
    public string ProofOfAddressFileName { get; set; } = string.Empty;
    public string ProofOfAddressContentType { get; set; } = string.Empty;
    public byte[] ProofOfAddressContent { get; set; } = [];

    /// <summary>Read-only, so EF Core's default conventions never try to map it as a
    /// column - display-only convenience for the places (admin review, snackbars) that
    /// want "the name" rather than its parts.</summary>
    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";
}
