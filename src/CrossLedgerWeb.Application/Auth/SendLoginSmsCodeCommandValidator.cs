using FluentValidation;

namespace CrossLedgerWeb.Application.Auth;

public sealed class SendLoginSmsCodeCommandValidator : AbstractValidator<SendLoginSmsCodeCommand>
{
    public SendLoginSmsCodeCommandValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty();
    }
}
