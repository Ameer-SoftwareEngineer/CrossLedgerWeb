using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public sealed record TwoFactorChallengeValidationResult(bool IsValid, UserId? UserId)
{
    public static TwoFactorChallengeValidationResult Invalid { get; } = new(false, null);

    public static TwoFactorChallengeValidationResult Valid(UserId userId) => new(true, userId);
}

/// <summary>The write side of the login-time 2FA challenge token - mirrors
/// IStepUpTokenValidator's own reason for existing: the handlers that consume a
/// challenge token depend on this, not on a JWT library directly.</summary>
public interface ITwoFactorChallengeTokenValidator
{
    TwoFactorChallengeValidationResult Validate(string token);
}
