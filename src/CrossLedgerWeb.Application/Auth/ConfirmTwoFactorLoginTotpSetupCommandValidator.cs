using FluentValidation;

namespace CrossLedgerWeb.Application.Auth;

public sealed class ConfirmTwoFactorLoginTotpSetupCommandValidator : AbstractValidator<ConfirmTwoFactorLoginTotpSetupCommand>
{
    public ConfirmTwoFactorLoginTotpSetupCommandValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty();
        RuleFor(x => x.Secret).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]+$");
    }
}
