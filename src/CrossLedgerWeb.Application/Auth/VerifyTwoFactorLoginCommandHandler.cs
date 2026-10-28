using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>The second step of every login once a method is already enrolled - mirrors
/// RequestStepUpTokenCommandHandler's TOTP replay-then-verify ordering for the Totp
/// branch, and delegates to IIdentityService for the Sms branch, which also marks the
/// phone confirmed on its first-ever successful verification.</summary>
public sealed class VerifyTwoFactorLoginCommandHandler : IRequestHandler<VerifyTwoFactorLoginCommand, LoginResult>
{
    private static readonly TimeSpan UsedCodeRetention = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TrustedDeviceValidity = TimeSpan.FromDays(30);

    private readonly ITwoFactorChallengeTokenValidator _challengeValidator;
    private readonly IIdentityService _identity;
    private readonly ITwoFactorCredentialRepository _credentials;
    private readonly IUsedTotpCodeRepository _usedCodes;
    private readonly ITotpProvider _totp;
    private readonly ILoginTokenIssuer _tokenIssuer;
    private readonly ITrustedDeviceRepository _trustedDevices;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public VerifyTwoFactorLoginCommandHandler(
        ITwoFactorChallengeTokenValidator challengeValidator,
        IIdentityService identity,
        ITwoFactorCredentialRepository credentials,
        IUsedTotpCodeRepository usedCodes,
        ITotpProvider totp,
        ILoginTokenIssuer tokenIssuer,
        ITrustedDeviceRepository trustedDevices,
        ISecurityAuditLogRepository auditLog,
        IRequestContextAccessor requestContext,
        IClock clock)
    {
        _challengeValidator = challengeValidator;
        _identity = identity;
        _credentials = credentials;
        _usedCodes = usedCodes;
        _totp = totp;
        _tokenIssuer = tokenIssuer;
        _trustedDevices = trustedDevices;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<LoginResult> Handle(VerifyTwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        var validation = _challengeValidator.Validate(request.ChallengeToken);
        if (!validation.IsValid)
            throw new InvalidTwoFactorChallengeException();

        var userId = validation.UserId!.Value;
        var now = _clock.UtcNow;

        if (request.Method == TwoFactorMethods.Totp)
        {
            var credential = await _credentials.GetByUserIdAsync(userId, cancellationToken);
            if (credential is null || !credential.IsEnabled)
                throw new TwoFactorNotEnabledException();

            if (await _usedCodes.IsActiveAsync(userId, request.Code, now, cancellationToken))
            {
                await RecordAsync(userId, succeeded: false, "CodeReplayed", cancellationToken);
                throw new InvalidTwoFactorCodeException();
            }

            if (!_totp.VerifyCode(credential.Secret, request.Code))
            {
                await RecordAsync(userId, succeeded: false, "InvalidCode", cancellationToken);
                throw new InvalidTwoFactorCodeException();
            }

            _usedCodes.Add(new UsedTotpCode(UsedTotpCodeId.New(), userId, request.Code, now, now + UsedCodeRetention));
        }
        else if (request.Method == TwoFactorMethods.Sms)
        {
            var verified = await _identity.VerifyTwoFactorSmsCodeAsync(userId, request.Code, cancellationToken);
            if (!verified)
            {
                await RecordAsync(userId, succeeded: false, "InvalidCode", cancellationToken);
                throw new InvalidTwoFactorCodeException();
            }
        }
        else if (request.Method == TwoFactorMethods.TrustedDevice)
        {
            var device = await _trustedDevices.GetByTokenHashAsync(RefreshTokenGenerator.Hash(request.Code), cancellationToken);
            if (device is null || device.UserId != userId || !device.IsActive(now))
            {
                await RecordAsync(userId, succeeded: false, "InvalidOrExpiredTrustedDevice", cancellationToken);
                throw new InvalidTwoFactorCodeException();
            }

            device.MarkUsed(now);
        }
        else
        {
            throw new InvalidTwoFactorCodeException();
        }

        await RecordAsync(userId, succeeded: true, failureReason: null, cancellationToken);

        var profile = await _identity.GetProfileAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var result = _tokenIssuer.IssueTokens(userId, profile.Email, profile.Roles);

        // Only offered after an explicit Totp/Sms verification, never re-granted from an
        // already-trusted device - a 30-day grant is a fixed window from when the user
        // opted in, not a sliding one that silently renews itself forever.
        if (request.RememberDevice && request.Method != TwoFactorMethods.TrustedDevice)
        {
            var rawToken = RefreshTokenGenerator.GenerateRawToken();
            var deviceLabel = TrustedDeviceLabeler.Describe(_requestContext.UserAgent);
            var device = new TrustedDevice(
                TrustedDeviceId.New(), userId, RefreshTokenGenerator.Hash(rawToken), deviceLabel, now, TrustedDeviceValidity);
            _trustedDevices.Add(device);
            result = result with { TrustedDeviceToken = rawToken };

            await _auditLog.RecordAsync(
                new SecurityAuditEvent(
                    SecurityAuditEventId.New(), userId, deviceLabel, SecurityEventType.TrustedDeviceGranted, succeeded: true,
                    failureReason: null, _requestContext.IpAddress, _requestContext.UserAgent, now),
                cancellationToken);
        }

        return result;
    }

    private Task RecordAsync(UserId userId, bool succeeded, string? failureReason, CancellationToken cancellationToken) =>
        _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), userId, userId.ToString(), SecurityEventType.TwoFactorChallenge, succeeded, failureReason,
                _requestContext.IpAddress, _requestContext.UserAgent, _clock.UtcNow),
            cancellationToken);
}
