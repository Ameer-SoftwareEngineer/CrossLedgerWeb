using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Shared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrossLedgerWeb.Api.Controllers;

/// <summary>TOTP enrolment and step-up (specification 6). Any authenticated user can
/// manage their own two-factor credential regardless of role - this isn't a
/// Customer-only feature, so there's no [Authorize(Roles = ...)] restriction beyond
/// requiring a valid access token.</summary>
[ApiController]
[Route("api/v1/auth/2fa")]
[Authorize]
public sealed class TwoFactorController : ControllerBase
{
    private readonly IMediator _mediator;

    public TwoFactorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Persists nothing - the client holds the returned secret until Confirm
    /// proves it can generate a valid code from it (enrolment integrity, 6.1).</summary>
    [HttpPost("enroll/begin")]
    [ProducesResponseType<BeginTotpEnrollmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BeginTotpEnrollmentResponse>> BeginEnrollment(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var email = User.GetEmail();
        if (email is null)
            return Unauthorized();

        var result = await _mediator.Send(new BeginTotpEnrollmentCommand(userId, email), cancellationToken);

        return Ok(new BeginTotpEnrollmentResponse(result.Secret, result.QrCodeUri));
    }

    [HttpPost("enroll/confirm")]
    [EnableRateLimiting(RateLimiterPolicies.TotpVerification)]
    [ProducesResponseType<ConfirmTotpEnrollmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfirmTotpEnrollmentResponse>> ConfirmEnrollment(
        ConfirmTotpEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new ConfirmTotpEnrollmentCommand(userId, request.Secret, request.Code), cancellationToken);

        return Ok(new ConfirmTotpEnrollmentResponse(result.RecoveryCodes));
    }

    /// <summary>Issues the short-lived, operation-scoped token that [RequireStepUp]
    /// checks for on protected endpoints (specification 6.3).</summary>
    [HttpPost("step-up")]
    [EnableRateLimiting(RateLimiterPolicies.TotpVerification)]
    [ProducesResponseType<StepUpTokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StepUpTokenResponse>> RequestStepUp(
        RequestStepUpTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (!Enum.TryParse<StepUpOperation>(request.Operation, ignoreCase: true, out var operation))
        {
            return BadRequest(new { code = "INVALID_OPERATION", message = $"Unknown step-up operation '{request.Operation}'." });
        }

        var result = await _mediator.Send(new RequestStepUpTokenCommand(userId, operation, request.Code), cancellationToken);

        return Ok(new StepUpTokenResponse(result.StepUpToken, result.ExpiresAt));
    }

    /// <summary>"Protects the protection itself" (specification 6.2) - disabling 2FA is
    /// itself step-up-gated.</summary>
    [HttpPost("disable")]
    [RequireStepUp(Operation = StepUpOperation.DisableTwoFactor)]
    public async Task<IActionResult> Disable(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        await _mediator.Send(new DisableTwoFactorCommand(userId), cancellationToken);

        return NoContent();
    }

    // ---- Login 2FA (specification 9) - the caller has no access token yet at this
    // point, only the challenge token LoginChallengeResponse gave it, so every action
    // below is [AllowAnonymous] and identifies the user through that token instead. ----

    [HttpPost("login/send-sms")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.TotpVerification)]
    public async Task<IActionResult> SendLoginSms(SendLoginSmsCodeRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SendLoginSmsCodeCommand(request.ChallengeToken), cancellationToken);
        return NoContent();
    }

    [HttpPost("login/verify")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.TotpVerification)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponse>> VerifyLogin(VerifyTwoFactorLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VerifyTwoFactorLoginCommand(request.ChallengeToken, request.Method, request.Code), cancellationToken);

        return Ok(ToTokenResponse(result));
    }

    [HttpPost("login/totp/begin")]
    [AllowAnonymous]
    [ProducesResponseType<BeginTotpEnrollmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BeginTotpEnrollmentResponse>> BeginLoginTotpSetup(
        BeginTwoFactorLoginTotpSetupRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new BeginTwoFactorLoginTotpSetupCommand(request.ChallengeToken), cancellationToken);
        return Ok(new BeginTotpEnrollmentResponse(result.Secret, result.QrCodeUri));
    }

    [HttpPost("login/totp/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterPolicies.TotpVerification)]
    [ProducesResponseType<TwoFactorLoginSetupResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorLoginSetupResponse>> ConfirmLoginTotpSetup(
        ConfirmTwoFactorLoginTotpSetupRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConfirmTwoFactorLoginTotpSetupCommand(request.ChallengeToken, request.Secret, request.Code), cancellationToken);

        return Ok(new TwoFactorLoginSetupResponse(ToTokenResponse(result.Tokens), result.RecoveryCodes));
    }

    private static TokenResponse ToTokenResponse(LoginResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken, result.RefreshTokenExpiresAt);
}
