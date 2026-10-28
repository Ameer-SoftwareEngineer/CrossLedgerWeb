using MediatR;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Method is one of TwoFactorMethods.Totp/Sms/TrustedDevice. RememberDevice
/// only has an effect on the Totp/Sms branches - checking it there issues a new
/// TrustedDevice grant (specification 6.1) alongside the login tokens; it's ignored on
/// the TrustedDevice branch itself, which only ever redeems an existing grant.</summary>
public sealed record VerifyTwoFactorLoginCommand(string ChallengeToken, string Method, string Code, bool RememberDevice = false) : IRequest<LoginResult>;
