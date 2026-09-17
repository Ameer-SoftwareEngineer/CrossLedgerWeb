using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed class GetTransactionHistoryQueryHandler : IRequestHandler<GetTransactionHistoryQuery, TransactionHistoryPage>
{
    private readonly IWalletRepository _wallets;
    private readonly ITransactionHistoryReader _reader;

    public GetTransactionHistoryQueryHandler(IWalletRepository wallets, ITransactionHistoryReader reader)
    {
        _wallets = wallets;
        _reader = reader;
    }

    public async Task<TransactionHistoryPage> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        var ownerId = await _wallets.GetOwnerIdAsync(request.WalletId, cancellationToken);

        // Same 404 whether the wallet doesn't exist or simply isn't the caller's -
        // otherwise the endpoint would leak which wallet ids belong to someone else.
        if (ownerId is null || ownerId != request.CallerId)
            throw new WalletNotFoundException(request.WalletId);

        return await _reader.GetPageAsync(
            request.WalletId, request.PageNumber, request.PageSize, request.FromDate, request.ToDate, cancellationToken);
    }
}
