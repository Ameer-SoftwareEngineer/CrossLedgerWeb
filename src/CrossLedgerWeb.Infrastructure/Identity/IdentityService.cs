using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace CrossLedgerWeb.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ISmsSender _smsSender;

    public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ISmsSender smsSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _smsSender = smsSender;
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

    public async Task<TwoFactorPhoneStatus> GetTwoFactorPhoneStatusAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return new TwoFactorPhoneStatus(false, null);

        return new TwoFactorPhoneStatus(user.PhoneNumberConfirmed, MaskPhoneNumber(user.PhoneNumber));
    }

    public async Task SendTwoFactorSmsCodeAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString())
            ?? throw new UserNotFoundException(userId);

        var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
        await _smsSender.SendAsync(user.PhoneNumber!, $"Your CrossLedger verification code is {code}", cancellationToken);
    }

    public async Task<bool> VerifyTwoFactorSmsCodeAsync(UserId userId, string code, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return false;

        var verified = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, code);
        if (!verified)
            return false;

        if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        return true;
    }

    private static string? MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
            return phoneNumber;

        return new string('•', phoneNumber.Length - 4) + phoneNumber[^4..];
    }
}
