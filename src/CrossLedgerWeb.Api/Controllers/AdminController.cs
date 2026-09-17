using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Shared.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.Controllers;

/// <summary>Specification 9's Admin Console - the RBAC proof: every action here is
/// Admin-only, and nothing anywhere else in this API requires the Admin role, so a
/// Customer-authenticated caller hitting any of these gets a clean 403, not a 404 or a
/// silently-scoped result.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("users")]
    [ProducesResponseType<IReadOnlyList<UserSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryResponse>>> ListUsers(CancellationToken cancellationToken)
    {
        var users = await _mediator.Send(new ListUsersQuery(), cancellationToken);

        var response = users
            .Select(u => new UserSummaryResponse(u.Id.Value, u.Email, u.Roles, u.IsLockedOut))
            .ToList();

        return Ok(response);
    }

    [HttpPut("users/{userId:guid}/roles")]
    public async Task<IActionResult> SetUserRoles(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetUserRolesCommand(new UserId(userId), request.Roles), cancellationToken);
        return NoContent();
    }

    [HttpGet("provider-health")]
    [ProducesResponseType<IReadOnlyList<ProviderHealthResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProviderHealthResponse>>> GetProviderHealth(CancellationToken cancellationToken)
    {
        var health = await _mediator.Send(new GetProviderHealthQuery(), cancellationToken);

        var response = health
            .Select(h => new ProviderHealthResponse(h.ProviderCode.ToString(), h.Status.ToString(), h.CheckedAt))
            .ToList();

        return Ok(response);
    }

    [HttpGet("webhook-events")]
    [ProducesResponseType<WebhookEventPageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WebhookEventPageResponse>> ListWebhookEvents(
        [FromQuery] int pageNumber, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var page = await _mediator.Send(new ListWebhookEventsQuery(pageNumber, pageSize), cancellationToken);

        var response = new WebhookEventPageResponse(
            page.Entries.Select(e => new WebhookEventResponse(e.ProviderCode, e.EventId, e.ProcessedAt)).ToList(),
            page.TotalCount,
            page.PageNumber,
            page.PageSize);

        return Ok(response);
    }
}
