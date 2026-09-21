using CrossLedgerWeb.Application.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CrossLedgerWeb.Application.Tests.Otc;

public class AcceptOtcRequestCommandValidatorTests
{
    private readonly AcceptOtcRequestCommandValidator _validator = new();

    private static AcceptOtcRequestCommand ValidCommand() => new(
        OtcRequestId.New(), UserId.New(), WalletId.New(), "idem-key");

    [Fact]
    public void A_well_formed_command_passes()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_empty_otc_request_id_fails()
    {
        var command = ValidCommand() with { OtcRequestId = default };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.OtcRequestId.Value);
    }

    [Fact]
    public void An_empty_idempotency_key_fails()
    {
        var command = ValidCommand() with { IdempotencyKey = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }
}
