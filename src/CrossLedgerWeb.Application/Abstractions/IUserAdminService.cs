using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>The boundary around ASP.NET Core Identity's UserManager/RoleManager for
/// admin-facing user management (specification 9's Admin Console, the RBAC proof) -
/// mirrors IIdentityService's own reason for existing: Application depends on this, never
/// on UserManager directly.</summary>
public interface IUserAdminService
{
    Task<IReadOnlyList<UserSummary>> ListUsersAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the user's role set with exactly the roles given. Returns false
    /// if no user with this id exists.</summary>
    Task<bool> SetRolesAsync(UserId userId, IReadOnlyList<string> roles, CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingRegistration>> ListPendingRegistrationsAsync(CancellationToken cancellationToken);

    /// <summary>Returns false if no user with this id exists.</summary>
    Task<bool> ApproveRegistrationAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Returns false if no user with this id exists.</summary>
    Task<bool> RejectRegistrationAsync(UserId userId, CancellationToken cancellationToken);

    Task<KycDocument?> GetKycDocumentAsync(UserId userId, CancellationToken cancellationToken);
}
