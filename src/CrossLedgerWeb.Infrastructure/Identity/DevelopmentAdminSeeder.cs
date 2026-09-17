using CrossLedgerWeb.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace CrossLedgerWeb.Infrastructure.Identity;

/// <summary>Guarantees a working Admin login exists locally so the Admin Console's role
/// separation (specification 6, 9's "RBAC proof") can actually be exercised without a
/// manual database edit - nothing else in this codebase ever grants Admin or Support;
/// every self-registration hardcodes Customer (see IdentityService.RegisterAsync).
/// Called only when the host environment is Development (checked at the call site in
/// Program.cs) - this well-known email/password pair must never exist outside a local
/// dev database.</summary>
public static class DevelopmentAdminSeeder
{
    public const string Email = "admin@crossledger.local";
    public const string Password = "DevAdmin@12345";

    public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(Email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = Email, Email = Email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, Password);
            if (!result.Succeeded)
                return;
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
            await userManager.AddToRoleAsync(user, Roles.Admin);
    }
}
