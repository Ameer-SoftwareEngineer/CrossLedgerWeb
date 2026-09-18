using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace CrossLedgerWeb.Infrastructure.Identity;

/// <summary>Guarantees a working Admin login exists locally so the Admin Console's role
/// separation (specification 6, 9's "RBAC proof") can actually be exercised without a
/// manual database edit - nothing else in this codebase ever grants Admin or Support;
/// every self-registration hardcodes Customer (see IdentityService.RegisterAsync).
/// Called only when the host environment is Development (checked at the call site in
/// Program.cs) - this well-known email/password pair must never exist outside a local
/// dev database. Pre-approved (RegistrationStatus.Approved) since the account needs to
/// sign in immediately, not sit behind the same KYC review it exists to operate. Also
/// pre-enrolled in both login 2FA methods with a well-known TOTP secret and a
/// pre-confirmed phone number - 2FA is mandatory for every account (specification 9), so
/// without this the seeded admin could never actually complete a login.</summary>
public static class DevelopmentAdminSeeder
{
    public const string Email = "admin@crossledger.local";
    public const string Password = "DevAdmin@12345";

    /// <summary>A fixed, publicly-documented Base32 secret - never generated per-run -
    /// so anyone with a TOTP app (or this codebase's tests) can compute a valid code for
    /// this account without ever seeing the enrollment QR flow.</summary>
    public const string TotpSecret = "CROSSLEDGERDEVSECRET234";

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager, ITwoFactorCredentialRepository twoFactorCredentials, IUnitOfWork unitOfWork)
    {
        var user = await userManager.FindByEmailAsync(Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                EmailConfirmed = true,
                PhoneNumber = "+10000000000",
                PhoneNumberConfirmed = true,
                FullName = "Dev Admin",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Address = "N/A",
                PermanentAddress = "N/A",
                City = "N/A",
                StateProvince = "N/A",
                Country = "N/A",
                RegistrationStatus = RegistrationStatus.Approved,
                RegistrationSubmittedAt = DateTimeOffset.UtcNow,
                ProofOfAddressDocumentType = "N/A",
                ProofOfAddressFileName = "N/A",
                ProofOfAddressContentType = "application/pdf",
                ProofOfAddressContent = [0x25, 0x50, 0x44, 0x46],
            };
            var result = await userManager.CreateAsync(user, Password);
            if (!result.Succeeded)
                return;
        }
        else
        {
            var changed = false;

            if (user.RegistrationStatus != RegistrationStatus.Approved)
            {
                user.RegistrationStatus = RegistrationStatus.Approved;
                changed = true;
            }

            // Backfills a row seeded before PhoneNumber/2FA existed on this account -
            // otherwise SendTwoFactorSmsCodeAsync would be sending to a null number for
            // every admin account created before this field was added.
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                user.PhoneNumber = "+10000000000";
                changed = true;
            }

            if (!user.PhoneNumberConfirmed)
            {
                user.PhoneNumberConfirmed = true;
                changed = true;
            }

            if (changed)
                await userManager.UpdateAsync(user);
        }

        var userId = new UserId(user.Id);
        if (await twoFactorCredentials.GetByUserIdAsync(userId, CancellationToken.None) is null)
        {
            twoFactorCredentials.Add(new TwoFactorCredential(
                TwoFactorCredentialId.New(), userId, TotpSecret, DateTimeOffset.UtcNow));
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
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
