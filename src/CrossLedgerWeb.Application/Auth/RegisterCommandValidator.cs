using FluentValidation;

namespace CrossLedgerWeb.Application.Auth;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public const long MaxDocumentSizeBytes = 5 * 1024 * 1024;
    public const string RequiredDocumentContentType = "application/pdf";

    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        // The authoritative complexity policy lives in Identity's PasswordOptions
        // (Infrastructure) - this just rejects an obviously-too-short password before
        // it reaches that layer at all.
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PermanentAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StateProvince).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);

        // KYC: an account holder must be an adult, verified against today's date rather
        // than trusting a client-supplied "over 18" flag.
        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
            .WithMessage("You must be at least 18 years old to register.");

        RuleFor(x => x.ProofOfAddressDocumentType).NotEmpty();
        RuleFor(x => x.ProofOfAddressFileName).NotEmpty();

        RuleFor(x => x.ProofOfAddressContentType)
            .Equal(RequiredDocumentContentType)
            .WithMessage("Proof of address must be a PDF file.");

        RuleFor(x => x.ProofOfAddressContent)
            .Must(content => content.Length > 0)
            .WithMessage("Proof of address document is required.")
            .Must(content => content.Length <= MaxDocumentSizeBytes)
            .WithMessage("Proof of address document must be 5 MB or smaller.");
    }
}
