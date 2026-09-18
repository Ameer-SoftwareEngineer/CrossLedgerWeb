using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record SendLoginSmsCodeCommand(string ChallengeToken) : IRequest;
