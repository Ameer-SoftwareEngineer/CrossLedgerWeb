using CrossLedgerWeb.Application.Auth;
using FluentValidation;

namespace CrossLedgerWeb.Application.Admin;

public sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(x => x.Roles).NotNull();
        RuleForEach(x => x.Roles).Must(role => Roles.All.Contains(role))
            .WithMessage("Every role must be one of: " + string.Join(", ", Roles.All));
    }
}
