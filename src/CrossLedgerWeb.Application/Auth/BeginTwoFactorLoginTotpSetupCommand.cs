using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record BeginTwoFactorLoginTotpSetupCommand(string ChallengeToken) : IRequest<BeginTotpEnrollmentResult>;
