using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>
/// The boundary around ASP.NET Core Identity: Application depends on this, never on
/// UserManager/SignInManager directly, so the password hashing scheme, lockout policy
/// and user store stay Infrastructure's concern (specification 6.1).
/// </summary>
public interface IIdentityService
{
    Task<RegistrationOutcome> RegisterAsync(RegistrationDetails details, CancellationToken cancellationToken);

    Task<CredentialValidationOutcome> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    Task<UserProfile?> GetProfileAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Whether the user's phone number has completed SMS two-factor
    /// verification at least once (specification 9's login 2FA) - backed by Identity's
    /// own PhoneNumberConfirmed, so no separate "SMS credential" table is needed.</summary>
    Task<TwoFactorPhoneStatus> GetTwoFactorPhoneStatusAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Generates a fresh code via Identity's built-in phone token provider and
    /// hands it to ISmsSender - a new code every call, never reused across logins.</summary>
    Task SendTwoFactorSmsCodeAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Verifies a code from SendTwoFactorSmsCodeAsync. On the first successful
    /// verification ever for this user, also marks the phone as confirmed - completing
    /// SMS 2FA enrolment and this verification in the same step.</summary>
    Task<bool> VerifyTwoFactorSmsCodeAsync(UserId userId, string code, CancellationToken cancellationToken);
}

public sealed record TwoFactorPhoneStatus(bool IsVerified, string? MaskedPhoneNumber);
