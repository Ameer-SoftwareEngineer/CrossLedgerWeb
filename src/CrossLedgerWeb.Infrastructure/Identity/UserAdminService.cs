using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
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
}
