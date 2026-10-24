using FluentValidation;

namespace CrossLedgerWeb.Application.Cards;

public sealed class TopUpCardCommandValidator : AbstractValidator<TopUpCardCommand>
{
    public TopUpCardCommandValidator()
    {
        RuleFor(x => x.CardId.Value).NotEmpty();
        RuleFor(x => x.SourceWalletId.Value).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
