using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Issues a step-up token (specification 6.1) only after a fresh, unused TOTP
/// code verifies against the caller's enrolled secret. Order matters: the replay check
/// runs before VerifyCode so a replayed code is rejected without a second (comparatively
/// expensive) TOTP computation, and the code is only recorded as used once it has
/// actually verified - a wrong code must remain retryable.</summary>
public sealed class RequestStepUpTokenCommandHandler : IRequestHandler<RequestStepUpTokenCommand, StepUpTokenResult>
{
    // Covers a standard 30-second TOTP step plus the +/-1 step skew tolerance
    // ITotpProvider implementations typically allow, with margin.
    private static readonly TimeSpan UsedCodeRetention = TimeSpan.FromMinutes(2);

    private readonly ITwoFactorCredentialRepository _credentials;
    private readonly IUsedTotpCodeRepository _usedCodes;
    private readonly ITotpProvider _totp;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public RequestStepUpTokenCommandHandler(
        ITwoFactorCredentialRepository credentials,
        IUsedTotpCodeRepository usedCodes,
        ITotpProvider totp,
        IJwtTokenGenerator jwtTokenGenerator,
        ISecurityAuditLogRepository auditLog,
        IRequestContextAccessor requestContext,
        IClock clock)
    {
        _credentials = credentials;
        _usedCodes = usedCodes;
        _totp = totp;
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<StepUpTokenResult> Handle(RequestStepUpTokenCommand request, CancellationToken cancellationToken)
    {
        var credential = await _credentials.GetByUserIdAsync(request.UserId, cancellationToken);
        if (credential is null || !credential.IsEnabled)
            throw new TwoFactorNotEnabledException();

        var now = _clock.UtcNow;

        if (await _usedCodes.IsActiveAsync(request.UserId, request.Code, now, cancellationToken))
        {
            await RecordAsync(request, succeeded: false, "CodeReplayed", cancellationToken);
            throw new TotpCodeReplayedException();
        }

        if (!_totp.VerifyCode(credential.Secret, request.Code))
        {
            await RecordAsync(request, succeeded: false, "InvalidCode", cancellationToken);
            throw new InvalidTotpCodeException();
        }

        _usedCodes.Add(new UsedTotpCode(UsedTotpCodeId.New(), request.UserId, request.Code, now, now + UsedCodeRetention));
        await RecordAsync(request, succeeded: true, failureReason: null, cancellationToken);

        var token = _jwtTokenGenerator.GenerateStepUpToken(request.UserId, request.Operation);

        return new StepUpTokenResult(token.Value, token.ExpiresAt);
    }

    private Task RecordAsync(RequestStepUpTokenCommand request, bool succeeded, string? failureReason, CancellationToken cancellationToken) =>
        _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.UserId, $"{request.UserId} ({request.Operation})", SecurityEventType.StepUpToken,
                succeeded, failureReason, _requestContext.IpAddress, _requestContext.UserAgent, _clock.UtcNow),
            cancellationToken);
}
