namespace CrossLedgerWeb.Application.Auth;

/// <summary>The two login 2FA methods a user can choose between (specification 9) -
/// plain strings on the wire, matching how StepUpOperation is already exposed as a
/// string in RequestStepUpTokenRequest rather than a numeric enum.</summary>
public static class TwoFactorMethods
{
    public const string Totp = "Totp";
    public const string Sms = "Sms";

    public static readonly IReadOnlyList<string> All = [Totp, Sms];
}
