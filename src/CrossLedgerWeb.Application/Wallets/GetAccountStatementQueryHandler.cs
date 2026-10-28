using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed class GetAccountStatementQueryHandler : IRequestHandler<GetAccountStatementQuery, AccountStatement>
{
    private readonly IWalletRepository _wallets;
    private readonly IAccountStatementReader _reader;

    public GetAccountStatementQueryHandler(IWalletRepository wallets, IAccountStatementReader reader)
    {
        _wallets = wallets;
        _reader = reader;
    }

    public async Task<AccountStatement> Handle(GetAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var ownership = await _wallets.GetOwnershipAsync(request.WalletId, cancellationToken);

        // Same 404 whether the wallet doesn't exist or simply isn't the caller's,
        // matching GetTransactionHistoryQueryHandler.
        if (ownership is null || ownership.OwnerId != request.CallerId)
            throw new WalletNotFoundException(request.WalletId);

        return await _reader.GetStatementAsync(request.WalletId, ownership.Currency, request.FromDate, request.ToDate, cancellationToken);
    }
}
