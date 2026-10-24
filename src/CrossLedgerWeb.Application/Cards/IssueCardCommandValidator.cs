using FluentValidation;

namespace CrossLedgerWeb.Application.Cards;

public sealed class IssueCardCommandValidator : AbstractValidator<IssueCardCommand>
{
    public IssueCardCommandValidator()
    {
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.CardholderName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EstimatedGrossAnnualIncome).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.EstimatedNetWorth).GreaterThanOrEqualTo(0m);

        RuleFor(x => x.RegisteredAddress.Street).NotEmpty();
        RuleFor(x => x.RegisteredAddress.State).NotEmpty();
        RuleFor(x => x.RegisteredAddress.Postcode).NotEmpty();
        RuleFor(x => x.RegisteredAddress.City).NotEmpty();
        RuleFor(x => x.RegisteredAddress.Country).NotEmpty();

        When(x => x.ShippingAddress is not null, () =>
        {
            RuleFor(x => x.ShippingAddress!.Street).NotEmpty();
            RuleFor(x => x.ShippingAddress!.State).NotEmpty();
            RuleFor(x => x.ShippingAddress!.Postcode).NotEmpty();
            RuleFor(x => x.ShippingAddress!.City).NotEmpty();
            RuleFor(x => x.ShippingAddress!.Country).NotEmpty();
        });
    }
}
