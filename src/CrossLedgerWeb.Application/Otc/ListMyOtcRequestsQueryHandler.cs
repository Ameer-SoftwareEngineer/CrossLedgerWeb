using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

public sealed class ListMyOtcRequestsQueryHandler : IRequestHandler<ListMyOtcRequestsQuery, IReadOnlyList<OtcRequestSummary>>
{
    private readonly IOtcRequestRepository _otcRequests;
    private readonly IClock _clock;

    public ListMyOtcRequestsQueryHandler(IOtcRequestRepository otcRequests, IClock clock)
    {
        _otcRequests = otcRequests;
        _clock = clock;
    }

    public async Task<IReadOnlyList<OtcRequestSummary>> Handle(ListMyOtcRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await _otcRequests.ListByRequesterAsync(request.RequestedByUserId, cancellationToken);
        var now = _clock.UtcNow;

        return requests
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new OtcRequestSummary(
                r.Id, r.QuoteId, r.SourceWalletId, r.FromCurrency, r.ToCurrency,
                r.RequestedAmount, r.DeskRate, r.RequestedAt, r.OfferExpiresAt, r.Status, r.TransferId, r.IsExpired(now)))
            .ToList();
    }
}
