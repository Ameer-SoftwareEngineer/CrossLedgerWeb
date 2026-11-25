namespace CrossLedgerWeb.Api.Security;

public static class RateLimiterPolicies
{
    /// <summary>Specification 6.4: "Rate limiting on verification" - applied to the TOTP
    /// enrolment-confirmation and step-up endpoints, both of which check a user-supplied
    /// code against a secret and so are the brute-force surface.</summary>
    public const string TotpVerification = "totp-verification";

    /// <summary>The password-check step of login (POST /api/v1/auth/login) - unauthenticated,
    /// so there is no sub claim to key on, and otherwise unlimited: Identity's own account
    /// lockout only stops repeated guesses against ONE known account, it does nothing to
    /// slow a credential-stuffing run across many different email addresses from one IP.
    /// Slightly more generous than TotpVerification's 5/5min since real users mistype
    /// passwords.</summary>
    public const string LoginAttempt = "login-attempt";

    /// <summary>Defense-in-depth on the whole Admin Console surface - not closing a specific
    /// exploit, just bounding how fast an already-authenticated Admin token can be used to
    /// hammer these endpoints.</summary>
    public const string AdminAction = "admin-action";
}
