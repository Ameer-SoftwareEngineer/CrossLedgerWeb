using FluentValidation;

namespace CrossLedgerWeb.Application.Cards;

public sealed class IssueCardCommandValidator : AbstractValidator<IssueCardCommand>
{
    public IssueCardCommandValidator()
    {
        RuleFor(x => x.LinkedWalletId.Value).NotEmpty();
        RuleFor(x => x.CardholderName).NotEmpty().MaximumLength(100);
    }
}
