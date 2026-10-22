using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class IssueCardCommandValidatorTests
{
    private readonly IssueCardCommandValidator _validator = new();

    private static IssueCardCommand ValidCommand() => new(UserId.New(), WalletId.New(), "Test Customer");

    [Fact]
    public void A_well_formed_command_passes()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_empty_linked_wallet_id_fails()
    {
        var command = ValidCommand() with { LinkedWalletId = default };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.LinkedWalletId.Value);
    }

    [Fact]
    public void An_empty_cardholder_name_fails()
    {
        var command = ValidCommand() with { CardholderName = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CardholderName);
    }
}
