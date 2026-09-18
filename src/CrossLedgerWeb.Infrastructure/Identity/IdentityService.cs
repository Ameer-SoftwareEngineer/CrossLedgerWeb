using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace CrossLedgerWeb.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<RegistrationOutcome> RegisterAsync(RegistrationDetails details, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = details.Email,
            Email = details.Email,
            PhoneNumber = details.PhoneNumber,
            FullName = details.FullName,
            DateOfBirth = details.DateOfBirth,
            Address = details.Address,
            PermanentAddress = details.PermanentAddress,
            City = details.City,
            StateProvince = details.StateProvince,
            Country = details.Country,
            RegistrationStatus = RegistrationStatus.Pending,
            RegistrationSubmittedAt = DateTimeOffset.UtcNow,
            ProofOfAddressDocumentType = details.ProofOfAddressDocumentType,
            ProofOfAddressFileName = details.ProofOfAddressFileName,
            ProofOfAddressContentType = details.ProofOfAddressContentType,
            ProofOfAddressContent = details.ProofOfAddressContent,
        };

        var result = await _userManager.CreateAsync(user, details.Password);

        if (!result.Succeeded)
            return RegistrationOutcome.Failure(result.Errors.Select(e => e.Description).ToList());

        await _userManager.AddToRoleAsync(user, Roles.Customer);

        return RegistrationOutcome.Success(new UserId(user.Id));
    }

    public async Task<CredentialValidationOutcome> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return CredentialValidationOutcome.Failed;

        // lockoutOnFailure: true delegates the increment-on-failure/reset-on-success
        // bookkeeping to Identity itself rather than this service re-implementing it.
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return CredentialValidationOutcome.LockedOut;

        return result.Succeeded
            ? CredentialValidationOutcome.Success(new UserId(user.Id))
            : CredentialValidationOutcome.Failed;
    }

    public async Task<UserProfile?> GetProfileAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfile(userId, user.Email!, roles.ToList(), user.RegistrationStatus);
    }
}
