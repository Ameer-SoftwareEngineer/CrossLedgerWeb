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

        // Also Customer, not just Admin - otherwise this account 403s on every
        // Customer-only page (Wallets, Send Money, ...) the moment it lands on Home
        // after logging in, which has nothing to do with proving RBAC and is just
        // friction for whoever uses this seeded account to explore the app.
        foreach (var role in new[] { Roles.Admin, Roles.Customer })
        {
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
