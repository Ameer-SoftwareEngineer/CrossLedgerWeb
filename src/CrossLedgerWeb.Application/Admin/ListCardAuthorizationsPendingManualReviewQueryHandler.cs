using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListCardAuthorizationsPendingManualReviewQueryHandler
    : IRequestHandler<ListCardAuthorizationsPendingManualReviewQuery, IReadOnlyList<CardAuthorizationPendingManualReview>>
{
    private readonly ICardAuthorizationRepository _authorizations;
    private readonly ICardRepository _cards;

    public ListCardAuthorizationsPendingManualReviewQueryHandler(ICardAuthorizationRepository authorizations, ICardRepository cards)
    {
        _authorizations = authorizations;
        _cards = cards;
    }

    public async Task<IReadOnlyList<CardAuthorizationPendingManualReview>> Handle(
        ListCardAuthorizationsPendingManualReviewQuery request, CancellationToken cancellationToken)
    {
        var pending = await _authorizations.ListPendingManualReviewAsync(cancellationToken);
        if (pending.Count == 0)
            return Array.Empty<CardAuthorizationPendingManualReview>();

        var cardholderNamesByCardId = new Dictionary<CardId, string>();
        var results = new List<CardAuthorizationPendingManualReview>(pending.Count);

        foreach (var authorization in pending)
        {
            if (!cardholderNamesByCardId.TryGetValue(authorization.CardId, out var cardholderName))
            {
                var card = await _cards.GetByIdAsync(authorization.CardId, cancellationToken);
                cardholderName = card?.CardholderName ?? "(unknown)";
                cardholderNamesByCardId[authorization.CardId] = cardholderName;
            }

            results.Add(new CardAuthorizationPendingManualReview(
                authorization.Id,
                authorization.CardId,
                cardholderName,
                authorization.MerchantName,
                authorization.Amount.Amount,
                authorization.Amount.Currency.Code,
                authorization.AttemptedCaptureAmount!.Value,
                authorization.ManualReviewReason!,
                authorization.DecidedAt,
                authorization.ManualReviewFlaggedAt!.Value));
        }

        return results;
    }
}
