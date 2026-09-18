using CrossLedgerWeb.Api.Models;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Shared.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
            request.FullName,
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

    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshAccessTokenCommand(request.RefreshToken), cancellationToken);

        return Ok(ToResponse(result));
    }

    private static TokenResponse ToResponse(LoginResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken, result.RefreshTokenExpiresAt);
}
