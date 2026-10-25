using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Validates credentials and mints the short-lived challenge that starts the
/// mandatory login 2FA step (specification 9) - never issues a real token pair itself
/// any more. See LoginTokenIssuer for the handlers that do (VerifyTwoFactorLoginCommand,
/// SendLoginSmsCodeCommand's sibling ConfirmTwoFactorLoginTotpSetupCommand).</summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginChallengeResult>
{
    public static readonly TimeSpan RefreshTokenValidity = TimeSpan.FromDays(30);

    private readonly IIdentityService _identity;
    private readonly ITwoFactorCredentialRepository _twoFactorCredentials;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public LoginCommandHandler(
        IIdentityService identity,
        ITwoFactorCredentialRepository twoFactorCredentials,
        IJwtTokenGenerator jwtTokenGenerator,
        ISecurityAuditLogRepository auditLog,
        IRequestContextAccessor requestContext,
        IClock clock)
    {
        _identity = identity;
        _twoFactorCredentials = twoFactorCredentials;
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<LoginChallengeResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validation = await _identity.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (validation.IsLockedOut)
        {
            await RecordAsync(validation.UserId, request.Email, succeeded: false, "AccountLocked", cancellationToken);
            throw new AccountLockedException(request.Email);
        }

        if (!validation.Succeeded)
        {
            await RecordAsync(validation.UserId, request.Email, succeeded: false, "InvalidCredentials", cancellationToken);
            throw new InvalidCredentialsException();
        }

        var userId = validation.UserId!.Value;
        await RecordAsync(userId, request.Email, succeeded: true, failureReason: null, cancellationToken);
        var profile = await _identity.GetProfileAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        // Credentials alone aren't enough - specification 9's Admin Console review must
        // clear the account before it can ever receive a token.
        if (profile.RegistrationStatus == RegistrationStatus.Pending)
            throw new AccountPendingApprovalException();

        if (profile.RegistrationStatus == RegistrationStatus.Rejected)
            throw new AccountRegistrationRejectedException();

        var totpCredential = await _twoFactorCredentials.GetByUserIdAsync(userId, cancellationToken);
        var phoneStatus = await _identity.GetTwoFactorPhoneStatusAsync(userId, cancellationToken);

        var enrolledMethods = new List<string>();
        if (totpCredential is { IsEnabled: true })
            enrolledMethods.Add(TwoFactorMethods.Totp);
        if (phoneStatus.IsVerified)
            enrolledMethods.Add(TwoFactorMethods.Sms);

        var requiresSetup = enrolledMethods.Count == 0;
        var availableMethods = requiresSetup ? TwoFactorMethods.All : enrolledMethods;

        var challengeToken = _jwtTokenGenerator.GenerateTwoFactorChallengeToken(userId);

        return new LoginChallengeResult(
            challengeToken.Value, challengeToken.ExpiresAt, requiresSetup, availableMethods, phoneStatus.MaskedPhoneNumber);
    }

    private Task RecordAsync(UserId? userId, string email, bool succeeded, string? failureReason, CancellationToken cancellationToken) =>
        _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), userId, email, SecurityEventType.LoginPassword, succeeded, failureReason,
                _requestContext.IpAddress, _requestContext.UserAgent, _clock.UtcNow),
            cancellationToken);
}
