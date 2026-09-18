using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Reuses ConfirmTotpEnrollmentCommand for the actual enrolment (secret
/// verification, credential + recovery codes persisted), then issues real tokens in the
/// same step - first-time setup and the login it was blocking both complete together.</summary>
public sealed class ConfirmTwoFactorLoginTotpSetupCommandHandler
    : IRequestHandler<ConfirmTwoFactorLoginTotpSetupCommand, TwoFactorLoginSetupResult>
{
    private readonly ITwoFactorChallengeTokenValidator _challengeValidator;
    private readonly IIdentityService _identity;
    private readonly ILoginTokenIssuer _tokenIssuer;
    private readonly IMediator _mediator;

    public ConfirmTwoFactorLoginTotpSetupCommandHandler(
        ITwoFactorChallengeTokenValidator challengeValidator,
        IIdentityService identity,
        ILoginTokenIssuer tokenIssuer,
        IMediator mediator)
    {
        _challengeValidator = challengeValidator;
        _identity = identity;
        _tokenIssuer = tokenIssuer;
        _mediator = mediator;
    }

    public async Task<TwoFactorLoginSetupResult> Handle(ConfirmTwoFactorLoginTotpSetupCommand request, CancellationToken cancellationToken)
    {
        var validation = _challengeValidator.Validate(request.ChallengeToken);
        if (!validation.IsValid)
            throw new InvalidTwoFactorChallengeException();

        var userId = validation.UserId!.Value;

        var enrollment = await _mediator.Send(
            new ConfirmTotpEnrollmentCommand(userId, request.Secret, request.Code), cancellationToken);

        var profile = await _identity.GetProfileAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var tokens = _tokenIssuer.IssueTokens(userId, profile.Email, profile.Roles);

        return new TwoFactorLoginSetupResult(tokens, enrollment.RecoveryCodes);
    }
}
