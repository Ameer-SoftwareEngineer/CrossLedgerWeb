using FluentValidation;

namespace CrossLedgerWeb.Application.Otc;

public sealed class AcceptOtcRequestCommandValidator : AbstractValidator<AcceptOtcRequestCommand>
{
    public AcceptOtcRequestCommandValidator()
    {
        RuleFor(x => x.OtcRequestId.Value).NotEmpty();
        RuleFor(x => x.TargetWalletId.Value).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
