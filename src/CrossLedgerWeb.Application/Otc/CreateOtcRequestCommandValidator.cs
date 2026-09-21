using FluentValidation;

namespace CrossLedgerWeb.Application.Otc;

public sealed class CreateOtcRequestCommandValidator : AbstractValidator<CreateOtcRequestCommand>
{
    /// <summary>What separates an OTC desk request from a regular Send Money conversion -
    /// below this, the retail quote flow already serves the customer fine.</summary>
    public const decimal MinimumAmount = 10_000m;

    public CreateOtcRequestCommandValidator()
    {
        RuleFor(x => x.SourceWalletId.Value).NotEmpty();
        RuleFor(x => x.FromCurrency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.ToCurrency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.ToCurrency).NotEqual(x => x.FromCurrency).WithMessage("An OTC request requires two different currencies.");
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(MinimumAmount)
            .WithMessage($"OTC desk requests start at {MinimumAmount:N0} - smaller amounts should use Send Money instead.");
    }
}
