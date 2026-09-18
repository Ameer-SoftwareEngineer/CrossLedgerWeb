using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Auth;

public sealed class LoginTokenIssuer : ILoginTokenIssuer
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IClock _clock;

    public LoginTokenIssuer(IJwtTokenGenerator jwtTokenGenerator, IRefreshTokenRepository refreshTokens, IClock clock)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public LoginResult IssueTokens(UserId userId, string email, IReadOnlyList<string> roles)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(userId, email, roles);

        var now = _clock.UtcNow;
        var rawRefreshToken = RefreshTokenGenerator.GenerateRawToken();
        var refreshToken = new RefreshToken(
            RefreshTokenId.New(), userId, RefreshTokenGenerator.Hash(rawRefreshToken), Guid.NewGuid(), now, LoginCommandHandler.RefreshTokenValidity);
        _refreshTokens.Add(refreshToken);

        return new LoginResult(accessToken.Value, accessToken.ExpiresAt, rawRefreshToken, refreshToken.ExpiresAt);
    }
}
