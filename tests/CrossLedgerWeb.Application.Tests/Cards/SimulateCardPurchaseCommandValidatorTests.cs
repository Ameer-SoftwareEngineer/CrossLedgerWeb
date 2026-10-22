using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class SimulateCardPurchaseCommandValidatorTests
{
    private readonly SimulateCardPurchaseCommandValidator _validator = new();

    private static SimulateCardPurchaseCommand ValidCommand() =>
        new(CardId.New(), UserId.New(), "Coffee Shop", 40m, "idem-key");

    [Fact]
    public void A_well_formed_command_passes()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void A_non_positive_amount_fails(decimal amount)
    {
        var command = ValidCommand() with { Amount = amount };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void An_empty_merchant_name_fails()
    {
        var command = ValidCommand() with { MerchantName = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.MerchantName);
    }

    [Fact]
    public void An_empty_idempotency_key_fails()
    {
        var command = ValidCommand() with { IdempotencyKey = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }
}
