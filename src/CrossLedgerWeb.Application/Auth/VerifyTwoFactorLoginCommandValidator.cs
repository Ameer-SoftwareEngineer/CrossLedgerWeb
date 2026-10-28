using FluentValidation;

namespace CrossLedgerWeb.Application.Auth;

public sealed class VerifyTwoFactorLoginCommandValidator : AbstractValidator<VerifyTwoFactorLoginCommand>
{
    public VerifyTwoFactorLoginCommandValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty();
        RuleFor(x => x.Method).NotEmpty().Must(m => m is TwoFactorMethods.Totp or TwoFactorMethods.Sms or TwoFactorMethods.TrustedDevice)
            .WithMessage("Method must be 'Totp', 'Sms' or 'TrustedDevice'.");

        // Totp/Sms codes are always a 6-digit code; a TrustedDevice "code" is actually
        // the opaque, high-entropy raw grant token from a previous login, so it gets no
        // digit/length constraint beyond simply being present.
        When(x => x.Method is TwoFactorMethods.Totp or TwoFactorMethods.Sms, () =>
            RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]+$"));
        When(x => x.Method == TwoFactorMethods.TrustedDevice, () =>
            RuleFor(x => x.Code).NotEmpty());
    }
}
