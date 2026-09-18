using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Auth;

/// <summary>Mints a fresh access/refresh token pair (specification 6.4) - the one piece
/// of work every "2FA just succeeded" handler needs (login verify, SMS verify, TOTP
/// setup-confirm), factored out so it's written once instead of four times.
/// Deliberately separate from RefreshAccessTokenCommandHandler's own token issuance,
/// which rotates within an existing refresh token family rather than starting a new
/// one.</summary>
public interface ILoginTokenIssuer
{
    LoginResult IssueTokens(UserId userId, string email, IReadOnlyList<string> roles);
}
