using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Identity;

public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserSummary>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);

        var summaries = new List<UserSummary>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            summaries.Add(new UserSummary(new UserId(user.Id), user.Email!, roles.ToList(), isLockedOut));
        }

        return summaries;
    }

    public async Task<bool> SetRolesAsync(UserId userId, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return false;

        var currentRoles = await _userManager.GetRolesAsync(user);
        var toAdd = roles.Except(currentRoles).ToList();
        var toRemove = currentRoles.Except(roles).ToList();

        if (toAdd.Count > 0)
            await _userManager.AddToRolesAsync(user, toAdd);

        if (toRemove.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, toRemove);

        return true;
    }

    public async Task<IReadOnlyList<PendingRegistration>> ListPendingRegistrationsAsync(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .Where(u => u.RegistrationStatus == RegistrationStatus.Pending)
            .OrderBy(u => u.RegistrationSubmittedAt)
            .ToListAsync(cancellationToken);

        return users
            .Select(u => new PendingRegistration(
                new UserId(u.Id),
                u.Email!,
                u.FirstName,
                u.MiddleName,
                u.LastName,
                u.PhoneNumber ?? string.Empty,
                u.DateOfBirth,
                u.Address,
                u.PermanentAddress,
                u.City,
                u.StateProvince,
                u.Country,
                u.ProofOfAddressDocumentType,
                u.ProofOfAddressFileName,
                u.RegistrationSubmittedAt))
            .ToList();
    }

    public async Task<bool> ApproveRegistrationAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return false;

        user.RegistrationStatus = RegistrationStatus.Approved;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> RejectRegistrationAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return false;

        user.RegistrationStatus = RegistrationStatus.Rejected;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<KycDocument?> GetKycDocumentAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || user.ProofOfAddressContent.Length == 0)
            return null;

        return new KycDocument(user.ProofOfAddressFileName, user.ProofOfAddressContentType, user.ProofOfAddressContent);
    }
}
