using FluentValidation;

namespace CrossLedgerWeb.Application.Auth;

public sealed class VerifyTwoFactorLoginCommandValidator : AbstractValidator<VerifyTwoFactorLoginCommand>
{
    public VerifyTwoFactorLoginCommandValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty();
        RuleFor(x => x.Method).NotEmpty().Must(m => m is TwoFactorMethods.Totp or TwoFactorMethods.Sms)
            .WithMessage("Method must be 'Totp' or 'Sms'.");
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]+$");
    }
}
