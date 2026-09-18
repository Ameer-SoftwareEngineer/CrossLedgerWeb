using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessToken GenerateAccessToken(UserId userId, string email, IReadOnlyList<string> roles);

    /// <summary>Short-lived (5 minutes, specification 6.1), scoped to exactly one
    /// operation class - a step-up token minted for a transfer can't be reused to
    /// authorize disabling two-factor auth.</summary>
    AccessToken GenerateStepUpToken(UserId userId, StepUpOperation operation);

    /// <summary>Identifies the user through the login 2FA step without granting API
    /// access - it carries no email/role claims, so it's useless as a bearer token even
    /// if it leaked. Validated by ITwoFactorChallengeTokenValidator, never by the normal
    /// [Authorize] pipeline.</summary>
    AccessToken GenerateTwoFactorChallengeToken(UserId userId);
}
