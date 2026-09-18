using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginChallengeResult>;

/// <summary>Login never returns tokens directly any more - 2FA is mandatory
/// (specification 9), so credentials alone only earn a short-lived challenge. RequiresSetup
/// tells the client whether AvailableMethods are methods the user can verify with right
/// now (already enrolled) or methods they can choose between to enrol for the first
/// time.</summary>
public sealed record LoginChallengeResult(
    string ChallengeToken,
    DateTimeOffset ChallengeExpiresAt,
    bool RequiresSetup,
    IReadOnlyList<string> AvailableMethods,
    string? MaskedPhoneNumber);

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
