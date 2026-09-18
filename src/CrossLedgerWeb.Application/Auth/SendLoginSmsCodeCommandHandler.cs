using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class SendLoginSmsCodeCommandHandler : IRequestHandler<SendLoginSmsCodeCommand>
{
    private readonly ITwoFactorChallengeTokenValidator _challengeValidator;
    private readonly IIdentityService _identity;

    public SendLoginSmsCodeCommandHandler(ITwoFactorChallengeTokenValidator challengeValidator, IIdentityService identity)
    {
        _challengeValidator = challengeValidator;
        _identity = identity;
    }

    public async Task Handle(SendLoginSmsCodeCommand request, CancellationToken cancellationToken)
    {
        var validation = _challengeValidator.Validate(request.ChallengeToken);
        if (!validation.IsValid)
            throw new InvalidTwoFactorChallengeException();

        await _identity.SendTwoFactorSmsCodeAsync(validation.UserId!.Value, cancellationToken);
    }
}
