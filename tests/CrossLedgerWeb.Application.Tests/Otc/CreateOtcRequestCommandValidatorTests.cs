using CrossLedgerWeb.Application.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CrossLedgerWeb.Application.Tests.Otc;

public class CreateOtcRequestCommandValidatorTests
{
    private readonly CreateOtcRequestCommandValidator _validator = new();

    private static CreateOtcRequestCommand ValidCommand() => new(
        UserId.New(), WalletId.New(), "USD", "PKR", CreateOtcRequestCommandValidator.MinimumAmount);

    [Fact]
    public void A_well_formed_command_passes()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Matching_currencies_fail()
    {
        var command = ValidCommand() with { ToCurrency = "USD" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ToCurrency);
    }

    [Fact]
    public void An_amount_below_the_otc_minimum_fails()
    {
        var command = ValidCommand() with { Amount = CreateOtcRequestCommandValidator.MinimumAmount - 0.01m };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void An_empty_source_wallet_id_fails()
    {
        var command = ValidCommand() with { SourceWalletId = default };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.SourceWalletId.Value);
    }
}
