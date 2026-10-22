using FluentValidation;

namespace CrossLedgerWeb.Application.Cards;

public sealed class SimulateCardPurchaseCommandValidator : AbstractValidator<SimulateCardPurchaseCommand>
{
    public SimulateCardPurchaseCommandValidator()
    {
        RuleFor(x => x.CardId.Value).NotEmpty();
        RuleFor(x => x.MerchantName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
