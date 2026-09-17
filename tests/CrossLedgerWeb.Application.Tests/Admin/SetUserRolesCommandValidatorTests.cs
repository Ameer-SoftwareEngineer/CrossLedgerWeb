using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class SetUserRolesCommandValidatorTests
{
    private readonly SetUserRolesCommandValidator _validator = new();

    [Fact]
    public void Validate_passes_for_known_roles()
    {
        var result = _validator.Validate(new SetUserRolesCommand(UserId.New(), new[] { "Customer", "Admin" }));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_for_an_unrecognised_role()
    {
        var result = _validator.Validate(new SetUserRolesCommand(UserId.New(), new[] { "SuperUser" }));

        result.IsValid.Should().BeFalse();
    }
}
