using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Reuses BeginTotpEnrollmentCommand's own logic via IMediator rather than
/// duplicating it - the only difference at login time is how the caller is identified
/// (a challenge token instead of an already-authenticated principal).</summary>
public sealed class BeginTwoFactorLoginTotpSetupCommandHandler : IRequestHandler<BeginTwoFactorLoginTotpSetupCommand, BeginTotpEnrollmentResult>
{
    private readonly ITwoFactorChallengeTokenValidator _challengeValidator;
    private readonly IIdentityService _identity;
    private readonly IMediator _mediator;

    public BeginTwoFactorLoginTotpSetupCommandHandler(
        ITwoFactorChallengeTokenValidator challengeValidator, IIdentityService identity, IMediator mediator)
    {
        _challengeValidator = challengeValidator;
        _identity = identity;
        _mediator = mediator;
    }

    public async Task<BeginTotpEnrollmentResult> Handle(BeginTwoFactorLoginTotpSetupCommand request, CancellationToken cancellationToken)
    {
        var validation = _challengeValidator.Validate(request.ChallengeToken);
        if (!validation.IsValid)
            throw new InvalidTwoFactorChallengeException();

        var userId = validation.UserId!.Value;
        var profile = await _identity.GetProfileAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        return await _mediator.Send(new BeginTotpEnrollmentCommand(userId, profile.Email), cancellationToken);
    }
}
