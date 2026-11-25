using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, Money>
{
    private readonly IWalletRepository _wallets;

    public GetWalletBalanceQueryHandler(IWalletRepository wallets)
    {
        _wallets = wallets;
    }

    public async Task<Money> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _wallets.GetByIdAsync(request.WalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.WalletId);

        // Same 404 whether the wallet doesn't exist or simply isn't the caller's,
        // matching GetTransactionHistoryQueryHandler/GetAccountStatementQueryHandler on
        // the same controller - without this, any authenticated customer could read any
        // other customer's wallet balance by guessing/learning its id.
        if (wallet.OwnerId != request.CallerId)
            throw new WalletNotFoundException(request.WalletId);

        return wallet.Balance;
    }
}
