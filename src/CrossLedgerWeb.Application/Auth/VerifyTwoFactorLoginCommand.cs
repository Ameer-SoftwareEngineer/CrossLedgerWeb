using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Method is one of TwoFactorMethods.Totp/Sms - the user's choice of which
/// already-enrolled method they're verifying with this login.</summary>
public sealed record VerifyTwoFactorLoginCommand(string ChallengeToken, string Method, string Code) : IRequest<LoginResult>;
