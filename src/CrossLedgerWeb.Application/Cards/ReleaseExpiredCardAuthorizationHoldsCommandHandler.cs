using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>No ledger involvement at all - an Open hold never posted a LedgerEntry, so
/// releasing one that expired uncaptured is purely a status flip on CardAuthorization
/// itself. Returns the count released, for the background service to log.</summary>
public sealed class ReleaseExpiredCardAuthorizationHoldsCommandHandler : IRequestHandler<ReleaseExpiredCardAuthorizationHoldsCommand, int>
{
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly IClock _clock;

    public ReleaseExpiredCardAuthorizationHoldsCommandHandler(ICardAuthorizationRepository authorizations, IClock clock)
    {
        _authorizations = authorizations;
        _clock = clock;
    }

    public async Task<int> Handle(ReleaseExpiredCardAuthorizationHoldsCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var expired = await _authorizations.ListOpenAndExpiredAsync(now, cancellationToken);

        foreach (var authorization in expired)
            authorization.Release(now);

        return expired.Count;
    }
}
