using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class IssueCardCommandValidatorTests
{
    private readonly IssueCardCommandValidator _validator = new();
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private static IssueCardCommand ValidCommand() => new(
        UserId.New(), "USD", CardType.Virtual, "Test Customer", SourceOfFunds.Salary, 5000m, 100000m, true,
        "Acme Inc", "Developer", TestAddress, null);

    [Fact]
    public void A_well_formed_command_passes()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_empty_currency_fails()
    {
        var command = ValidCommand() with { Currency = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void An_empty_cardholder_name_fails()
    {
        var command = ValidCommand() with { CardholderName = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CardholderName);
    }

    [Fact]
    public void A_negative_estimated_income_fails()
    {
        var command = ValidCommand() with { EstimatedGrossAnnualIncome = -1m };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.EstimatedGrossAnnualIncome);
    }

    [Fact]
    public void An_empty_registered_address_street_fails()
    {
        var command = ValidCommand() with { RegisteredAddress = TestAddress with { Street = "" } };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.RegisteredAddress.Street);
    }

    [Fact]
    public void An_empty_shipping_address_street_fails_when_shipping_address_is_given()
    {
        var command = ValidCommand() with { ShippingAddress = TestAddress with { Street = "" } };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ShippingAddress!.Street);
    }
}
