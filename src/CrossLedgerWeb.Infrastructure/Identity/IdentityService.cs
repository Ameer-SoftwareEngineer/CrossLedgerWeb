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
    private readonly IEmailSender _emailSender;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISmsSender smsSender,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _smsSender = smsSender;
        _emailSender = emailSender;
    }

    public async Task<RegistrationOutcome> RegisterAsync(RegistrationDetails details, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = details.Email,
            Email = details.Email,
            PhoneNumber = details.PhoneNumber,
            FirstName = details.FirstName,
            MiddleName = details.MiddleName,
            LastName = details.LastName,
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

    public async Task<MyProfile?> GetMyProfileAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return null;

        return new MyProfile(
            userId, user.Email!, user.FirstName, user.MiddleName, user.LastName, user.PhoneNumber ?? string.Empty,
            user.DateOfBirth, user.Address, user.PermanentAddress, user.City, user.StateProvince, user.Country,
            user.RegistrationStatus);
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

    /// <summary>Deliberately silent if the email doesn't match an account - the caller
    /// (RequestPasswordResetCommandHandler) always reports success either way, so this
    /// never becomes an account-enumeration oracle.</summary>
    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"{AppBaseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        await _emailSender.SendAsync(
            email,
            "Reset your CrossLedger password",
            $"Use this link to reset your password: {resetLink}",
            cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return false;

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    // No hosted frontend origin is configured anywhere in this project (it's a separate
    // repo/deployment) - hardcoded to the known local dev port, same spirit as
    // DevelopmentAdminSeeder's well-known dev-only values.
    private const string AppBaseUrl = "http://localhost:5180";

    private static string? MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
            return phoneNumber;

        return new string('•', phoneNumber.Length - 4) + phoneNumber[^4..];
    }
}
