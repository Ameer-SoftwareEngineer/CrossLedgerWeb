namespace CrossLedgerWeb.Domain.Security;

public enum SecurityEventType
{
    LoginPassword,
    TwoFactorChallenge,
    StepUpToken,
    TwoFactorDisabled,
    TrustedDeviceGranted,
    TrustedDeviceRevoked,

    // Admin actions below - unlike every event above (where the acting user and the
    // subject of the event are the same person), UserId here is the ACTING ADMIN, not
    // a target. Added so the Admin Console's own "Security Events" tab can finally
    // answer "who approved this" / "who changed this role" - previously nothing an
    // admin did was recorded anywhere queryable.
    RoleChanged,
    RegistrationApproved,
    RegistrationRejected,
    CardRequestApproved,
    CardRequestRejected,
    ManualTransferPosted,
    ManualReviewResolved,
    CardMarkedShipped,
    CardMarkedDelivered,
}
