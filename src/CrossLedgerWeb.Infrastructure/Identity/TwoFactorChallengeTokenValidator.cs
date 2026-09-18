using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CrossLedgerWeb.Infrastructure.Identity;

public sealed class TwoFactorChallengeTokenValidator : ITwoFactorChallengeTokenValidator
{
    private readonly IOptions<JwtOptions> _options;

    public TwoFactorChallengeTokenValidator(IOptions<JwtOptions> options)
    {
        _options = options;
    }

    public TwoFactorChallengeValidationResult Validate(string token)
    {
        var options = _options.Value;
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        System.Security.Claims.ClaimsPrincipal principal;
        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            principal = handler.ValidateToken(token, validationParameters, out _);
        }
        catch (Exception)
        {
            return TwoFactorChallengeValidationResult.Invalid;
        }

        var challengeClaim = principal.FindFirst(JwtTokenGenerator.TwoFactorChallengeClaimType)?.Value;
        var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (challengeClaim != "true" || subClaim is null)
            return TwoFactorChallengeValidationResult.Invalid;

        if (!Guid.TryParse(subClaim, out var userGuid))
            return TwoFactorChallengeValidationResult.Invalid;

        return TwoFactorChallengeValidationResult.Valid(new UserId(userGuid));
    }
}
