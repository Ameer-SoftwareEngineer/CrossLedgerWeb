using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Issues the token pair from specification 6.4: a short-lived access token
/// plus a rotating refresh token. The raw refresh token is returned to the caller once
/// and never again - only its hash is persisted (see RefreshTokenGenerator).</summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    public static readonly TimeSpan RefreshTokenValidity = TimeSpan.FromDays(30);

    private readonly IIdentityService _identity;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IClock _clock;

    public LoginCommandHandler(
        IIdentityService identity,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokens,
        IClock clock)
    {
        _identity = identity;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validation = await _identity.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (validation.IsLockedOut)
            throw new AccountLockedException(request.Email);

        if (!validation.Succeeded)
            throw new InvalidCredentialsException();

        var userId = validation.UserId!.Value;
        var profile = await _identity.GetProfileAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        // Credentials alone aren't enough - specification 9's Admin Console review must
        // clear the account before it can ever receive a token.
        if (profile.RegistrationStatus == RegistrationStatus.Pending)
            throw new AccountPendingApprovalException();

        if (profile.RegistrationStatus == RegistrationStatus.Rejected)
            throw new AccountRegistrationRejectedException();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(userId, profile.Email, profile.Roles);

        var now = _clock.UtcNow;
        var rawRefreshToken = RefreshTokenGenerator.GenerateRawToken();
        var refreshToken = new RefreshToken(
            RefreshTokenId.New(), userId, RefreshTokenGenerator.Hash(rawRefreshToken), Guid.NewGuid(), now, RefreshTokenValidity);
        _refreshTokens.Add(refreshToken);

        return new LoginResult(accessToken.Value, accessToken.ExpiresAt, rawRefreshToken, refreshToken.ExpiresAt);
    }
}
