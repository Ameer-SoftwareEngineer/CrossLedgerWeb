using FluentValidation;

namespace CrossLedgerWeb.Application.Auth;

public sealed class BeginTwoFactorLoginTotpSetupCommandValidator : AbstractValidator<BeginTwoFactorLoginTotpSetupCommand>
{
    public BeginTwoFactorLoginTotpSetupCommandValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty();
    }
}
