using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ResolveCardAuthorizationManualReviewCommandHandler : IRequestHandler<ResolveCardAuthorizationManualReviewCommand>
{
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly IClock _clock;

    public ResolveCardAuthorizationManualReviewCommandHandler(ICardAuthorizationRepository authorizations, IClock clock)
    {
        _authorizations = authorizations;
        _clock = clock;
    }

    public async Task Handle(ResolveCardAuthorizationManualReviewCommand request, CancellationToken cancellationToken)
    {
        var authorization = await _authorizations.GetByIdAsync(request.CardAuthorizationId, cancellationToken)
            ?? throw new CardAuthorizationNotFoundException(request.CardAuthorizationId);

        authorization.ResolveManualReview(_clock.UtcNow);
    }
}
