namespace CrossLedgerWeb.Domain.Security;

public enum SecurityEventType
{
    LoginPassword,
    TwoFactorChallenge,
    StepUpToken,
    TwoFactorDisabled,
    TrustedDeviceGranted,
    TrustedDeviceRevoked,
}
