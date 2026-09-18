using FluentValidation;

namespace CrossLedgerWeb.Application.Auth;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();

        // The authoritative complexity policy lives in Identity's PasswordOptions
        // (Infrastructure) - this just rejects an obviously-too-short password before
        // it reaches that layer at all.
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
