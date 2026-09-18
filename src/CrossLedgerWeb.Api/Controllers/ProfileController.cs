using CrossLedgerWeb.Api.Security;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Shared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

/// <summary>The "View Profile" surface any authenticated user (any role) can read about
/// themselves - not the Admin Console's KYC review, which reads someone else's
/// registration.</summary>
[ApiController]
[Route("api/v1/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    [ProducesResponseType<MyProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyProfileResponse>> GetMe(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var profile = await _mediator.Send(new GetMyProfileQuery(userId), cancellationToken);
        if (profile is null)
            return NotFound();

        return Ok(new MyProfileResponse(
            profile.UserId.Value,
            profile.Email,
            profile.FirstName,
            profile.MiddleName,
            profile.LastName,
            profile.PhoneNumber,
            profile.DateOfBirth,
            profile.Address,
            profile.PermanentAddress,
            profile.City,
            profile.StateProvince,
            profile.Country,
            profile.RegistrationStatus.ToString()));
    }
}
