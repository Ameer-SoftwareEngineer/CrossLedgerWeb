using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record ConfirmTwoFactorLoginTotpSetupCommand(string ChallengeToken, string Secret, string Code)
    : IRequest<TwoFactorLoginSetupResult>;

public sealed record TwoFactorLoginSetupResult(LoginResult Tokens, IReadOnlyList<string> RecoveryCodes);
