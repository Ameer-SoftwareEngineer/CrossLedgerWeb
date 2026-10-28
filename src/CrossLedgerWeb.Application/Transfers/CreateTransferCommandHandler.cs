using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Transfers;

/// <summary>
/// Mirrors the request flow in specification section 7.1: load the quote and wallets,
/// run the AML rolling-limit pre-check (dbo.usp_CheckTransferLimits), then let the
/// domain post the four ledger entries. Validation, idempotency and the atomic commit
/// (entries + idempotency record together) are pipeline behaviours, not this handler's
/// concern - it only stages domain changes on the tracked entities.
/// </summary>
public sealed class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, CreateTransferResult>
{
    private readonly IQuoteRepository _quotes;
    private readonly IWalletRepository _wallets;
    private readonly IFxSettlementWalletResolver _fxSettlementWallets;
    private readonly ITransferLimitsReader _transferLimits;
    private readonly IClock _clock;

    public CreateTransferCommandHandler(
        IQuoteRepository quotes,
        IWalletRepository wallets,
        IFxSettlementWalletResolver fxSettlementWallets,
        ITransferLimitsReader transferLimits,
        IClock clock)
    {
        _quotes = quotes;
        _wallets = wallets;
        _fxSettlementWallets = fxSettlementWallets;
        _transferLimits = transferLimits;
        _clock = clock;
    }

    public async Task<CreateTransferResult> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quotes.GetByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new QuoteNotFoundException(request.QuoteId);

        var sourceWallet = await _wallets.GetByIdAsync(request.SourceWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.SourceWalletId);

        var targetWallet = await _wallets.GetByIdAsync(request.TargetWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.TargetWalletId);

        var limitCheck = await _transferLimits.CheckAsync(
            sourceWallet.OwnerId, quote.FromCurrency, request.SourceAmount, cancellationToken);
        if (limitCheck.ExceedsLimit)
            throw new TransferLimitExceededException(sourceWallet.OwnerId, quote.FromCurrency, limitCheck.ProjectedTotal, limitCheck.RollingLimit);

        var fxSettlementSource = await _fxSettlementWallets.GetSettlementWalletAsync(quote.FromCurrency, cancellationToken);
        var fxSettlementTarget = await _fxSettlementWallets.GetSettlementWalletAsync(quote.ToCurrency, cancellationToken);

        var now = _clock.UtcNow;
        var transferId = TransferId.New();
        var sourceAmount = new Money(request.SourceAmount, quote.FromCurrency);

        var entries = TransferPoster.Post(
            transferId, quote,
            sourceWallet, fxSettlementSource, fxSettlementTarget, targetWallet,
            sourceAmount, now);

        var targetCreditEntry = entries[^1];

        return new CreateTransferResult(transferId, sourceAmount, targetCreditEntry.Amount, now);
    }
}
