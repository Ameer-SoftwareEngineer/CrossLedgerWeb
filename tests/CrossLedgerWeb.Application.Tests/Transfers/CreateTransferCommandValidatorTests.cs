using CrossLedgerWeb.Application.Transfers;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CrossLedgerWeb.Application.Tests.Transfers;

public class CreateTransferCommandValidatorTests
{
    private readonly CreateTransferCommandValidator _validator = new();

    private static CreateTransferCommand ValidCommand() => new(
        QuoteId.New(), WalletId.New(), WalletId.New(), 100m, UserId.New(), "idem-key");

    [Fact]
    public void A_well_formed_command_passes()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_empty_quote_id_fails()
    {
        var command = ValidCommand() with { QuoteId = default };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.QuoteId.Value);
    }

    [Fact]
    public void A_source_and_target_wallet_that_match_fails()
    {
        var walletId = WalletId.New();
        var command = ValidCommand() with { SourceWalletId = walletId, TargetWalletId = walletId };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.TargetWalletId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void A_non_positive_source_amount_fails(decimal amount)
    {
        var command = ValidCommand() with { SourceAmount = amount };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.SourceAmount);
    }

    [Fact]
    public void An_empty_idempotency_key_fails()
    {
        var command = ValidCommand() with { IdempotencyKey = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }
}
