using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>The expiry sweeper's own command (specification 12.3/12.4's "Settlement" week) -
/// releases every still-Open hold whose ExpiresAt has passed, run on a schedule by
/// ExpireCardAuthorizationHoldsBackgroundService.</summary>
public sealed record ReleaseExpiredCardAuthorizationHoldsCommand : IRequest<int>;
