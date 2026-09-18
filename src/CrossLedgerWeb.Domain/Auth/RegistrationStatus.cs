namespace CrossLedgerWeb.Domain.Auth;

/// <summary>A self-registered account cannot sign in until an admin reviews its KYC
/// details and proof-of-address document (specification 9's Admin Console) - Pending is
/// the only status IdentityService ever assigns at registration time.</summary>
public enum RegistrationStatus
{
    Pending,
    Approved,
    Rejected,
}
