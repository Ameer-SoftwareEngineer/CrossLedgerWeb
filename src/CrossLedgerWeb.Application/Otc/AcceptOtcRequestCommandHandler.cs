using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

/// <summary>Accepting a desk offer runs the exact same posting the regular Send Money
/// flow uses (CreateTransferCommandHandler) - the only difference is the Quote and
/// source amount come from the OtcRequest instead of the request body, and the request
/// itself is marked Accepted once the transfer has actually posted. A stale offer is
/// rejected by quote.Convert inside TransferPoster.Post (QuoteExpiredException), same as
/// an expired retail quote - no separate expiry check needed here.</summary>
public sealed class AcceptOtcRequestCommandHandler : IRequestHandler<AcceptOtcRequestCommand, AcceptOtcRequestResult>
{
    private readonly IOtcRequestRepository _otcRequests;
    private readonly IQuoteRepository _quotes;
    private readonly IWalletRepository _wallets;
    private readonly IFxSettlementWalletResolver _fxSettlementWallets;
    private readonly IClock _clock;

    public AcceptOtcRequestCommandHandler(
        IOtcRequestRepository otcRequests,
        IQuoteRepository quotes,
        IWalletRepository wallets,
        IFxSettlementWalletResolver fxSettlementWallets,
        IClock clock)
    {
        _otcRequests = otcRequests;
        _quotes = quotes;
        _wallets = wallets;
        _fxSettlementWallets = fxSettlementWallets;
        _clock = clock;
    }

    public async Task<AcceptOtcRequestResult> Handle(AcceptOtcRequestCommand request, CancellationToken cancellationToken)
    {
        var otcRequest = await _otcRequests.GetByIdAsync(request.OtcRequestId, cancellationToken)
            ?? throw new OtcRequestNotFoundException(request.OtcRequestId);

        // Same 404 whether the request doesn't exist or simply isn't the caller's.
        if (otcRequest.RequestedByUserId != request.CallerId)
            throw new OtcRequestNotFoundException(request.OtcRequestId);

        var quote = await _quotes.GetByIdAsync(otcRequest.QuoteId, cancellationToken)
            ?? throw new QuoteNotFoundException(otcRequest.QuoteId);

        var sourceWallet = await _wallets.GetByIdAsync(otcRequest.SourceWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(otcRequest.SourceWalletId);

        var targetWallet = await _wallets.GetByIdAsync(request.TargetWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.TargetWalletId);

        var fxSettlementSource = await _fxSettlementWallets.GetSettlementWalletAsync(quote.FromCurrency, cancellationToken);
        var fxSettlementTarget = await _fxSettlementWallets.GetSettlementWalletAsync(quote.ToCurrency, cancellationToken);

        var now = _clock.UtcNow;
        var transferId = TransferId.New();
        var sourceAmount = new Money(otcRequest.RequestedAmount, quote.FromCurrency);

        var entries = TransferPoster.Post(
            transferId, quote,
            sourceWallet, fxSettlementSource, fxSettlementTarget, targetWallet,
            sourceAmount, now);

        otcRequest.Accept(transferId.Value);

        var targetCreditEntry = entries[^1];

        return new AcceptOtcRequestResult(transferId, sourceAmount, targetCreditEntry.Amount, now);
    }
}
