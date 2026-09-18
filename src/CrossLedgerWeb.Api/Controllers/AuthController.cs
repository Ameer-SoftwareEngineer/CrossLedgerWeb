using CrossLedgerWeb.Api.Models;
using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Shared.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrossLedgerWeb.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<RegisterResponse>> Register([FromForm] RegisterFormRequest request, CancellationToken cancellationToken)
    {
        var documentContent = Array.Empty<byte>();
        if (request.ProofOfAddress is { Length: > 0 } document)
        {
            await using var stream = new MemoryStream();
            await document.CopyToAsync(stream, cancellationToken);
            documentContent = stream.ToArray();
        }

        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.MiddleName,
            request.LastName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Address,
            request.PermanentAddress,
            request.City,
            request.StateProvince,
            request.Country,
            request.ProofOfAddressDocumentType,
            request.ProofOfAddress?.FileName ?? string.Empty,
            request.ProofOfAddress?.ContentType ?? string.Empty,
            documentContent);

        var result = await _mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(result.UserId.Value, result.Email));
    }

    /// <summary>Credentials alone never return a token pair any more - 2FA is
    /// mandatory (specification 9), so this always returns a short-lived challenge that
    /// TwoFactorController's login/* endpoints consume to actually finish signing in.</summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginChallengeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginChallengeResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        return Ok(new LoginChallengeResponse(
            result.ChallengeToken, result.ChallengeExpiresAt, result.RequiresSetup, result.AvailableMethods, result.MaskedPhoneNumber));
    }

    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshAccessTokenCommand(request.RefreshToken), cancellationToken);

        return Ok(ToResponse(result));
    }

    /// <summary>Always 204, whether or not the email matches an account - the response
    /// itself must never be an oracle for account enumeration (specification 6.1).</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimiterPolicies.TotpVerification)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RequestPasswordResetCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimiterPolicies.TotpVerification)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), cancellationToken);
        return NoContent();
    }

    private static TokenResponse ToResponse(LoginResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken, result.RefreshTokenExpiresAt);
}
