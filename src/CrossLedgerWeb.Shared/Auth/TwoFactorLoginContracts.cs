namespace CrossLedgerWeb.Shared.Auth;

public sealed record LoginChallengeResponse(
    string ChallengeToken,
    DateTimeOffset ChallengeExpiresAt,
    bool RequiresSetup,
    IReadOnlyList<string> AvailableMethods,
    string? MaskedPhoneNumber);

public sealed record SendLoginSmsCodeRequest(string ChallengeToken);

/// <summary>Method is "Totp", "Sms" or "TrustedDevice". RememberDevice only has an
/// effect on the Totp/Sms branches - see VerifyTwoFactorLoginCommand.</summary>
public sealed record VerifyTwoFactorLoginRequest(string ChallengeToken, string Method, string Code, bool RememberDevice = false);

public sealed record BeginTwoFactorLoginTotpSetupRequest(string ChallengeToken);

public sealed record ConfirmTwoFactorLoginTotpSetupRequest(string ChallengeToken, string Secret, string Code);

public sealed record TwoFactorLoginSetupResponse(TokenResponse Tokens, IReadOnlyList<string> RecoveryCodes);
