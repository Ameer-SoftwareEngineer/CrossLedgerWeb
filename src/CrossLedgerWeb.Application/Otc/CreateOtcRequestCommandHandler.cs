using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

/// <summary>Simulates the OTC desk (specification 9's Ibanera-style dashboard) rather
/// than routing to a real trader: the "negotiated" offer is a Quote priced with a
/// tighter spread than CreateQuoteCommandHandler's retail 0.50%, held open for minutes
/// instead of seconds so the customer has time to review it. Accepting one just runs the
/// regular Quote -> transfer path, so no separate settlement code exists.</summary>
public sealed class CreateOtcRequestCommandHandler : IRequestHandler<CreateOtcRequestCommand, CreateOtcRequestResult>
{
    private static readonly decimal DeskSpreadRate = 0.0015m;
    private static readonly TimeSpan OfferValidity = TimeSpan.FromMinutes(5);

    private readonly IExchangeRateProvider _rates;
    private readonly IQuoteRepository _quotes;
    private readonly IOtcRequestRepository _otcRequests;
    private readonly IWalletRepository _wallets;
    private readonly IClock _clock;

    public CreateOtcRequestCommandHandler(
        IExchangeRateProvider rates,
        IQuoteRepository quotes,
        IOtcRequestRepository otcRequests,
        IWalletRepository wallets,
        IClock clock)
    {
        _rates = rates;
        _quotes = quotes;
        _otcRequests = otcRequests;
        _wallets = wallets;
        _clock = clock;
    }

    public async Task<CreateOtcRequestResult> Handle(CreateOtcRequestCommand request, CancellationToken cancellationToken)
    {
        // Same 404 whether the wallet doesn't exist or simply isn't the caller's, as
        // GetTransactionHistoryQueryHandler does - never leaks which wallet ids exist.
        var ownerId = await _wallets.GetOwnerIdAsync(request.SourceWalletId, cancellationToken);
        if (ownerId is null || ownerId != request.RequestedByUserId)
            throw new WalletNotFoundException(request.SourceWalletId);

        var from = Currency.From(request.FromCurrency);
        var to = Currency.From(request.ToCurrency);

        var reading = await _rates.GetRateAsync(from, to, cancellationToken);
        var now = _clock.UtcNow;

        var quote = new Quote(QuoteId.New(), from, to, reading.MidMarketRate, DeskSpreadRate, now, OfferValidity);
        _quotes.Add(quote);

        var otcRequest = new OtcRequest(
            OtcRequestId.New(), request.RequestedByUserId, request.SourceWalletId, quote.Id,
            from, to, request.Amount, quote.CustomerRate, now, quote.ExpiresAt);
        _otcRequests.Add(otcRequest);

        return new CreateOtcRequestResult(
            otcRequest.Id, quote.Id, otcRequest.SourceWalletId, from, to,
            otcRequest.RequestedAmount, otcRequest.DeskRate, otcRequest.RequestedAt, otcRequest.OfferExpiresAt);
    }
}
