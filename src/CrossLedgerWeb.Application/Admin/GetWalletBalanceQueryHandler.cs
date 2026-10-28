using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, WalletBalanceResult>
{
    private readonly IWalletBalanceReader _reader;

    public GetWalletBalanceQueryHandler(IWalletBalanceReader reader)
    {
        _reader = reader;
    }

    public async Task<WalletBalanceResult> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var balance = await _reader.GetBalanceAsync(request.WalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.WalletId);

        return new WalletBalanceResult(balance.Currency, balance.Balance);
    }
}
