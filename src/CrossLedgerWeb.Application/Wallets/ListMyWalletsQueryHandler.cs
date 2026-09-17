using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed class ListMyWalletsQueryHandler : IRequestHandler<ListMyWalletsQuery, IReadOnlyList<WalletSummary>>
{
    private readonly IWalletRepository _wallets;

    public ListMyWalletsQueryHandler(IWalletRepository wallets)
    {
        _wallets = wallets;
    }

    public async Task<IReadOnlyList<WalletSummary>> Handle(ListMyWalletsQuery request, CancellationToken cancellationToken)
    {
        var wallets = await _wallets.ListByOwnerAsync(request.OwnerId, cancellationToken);

        return wallets.Select(w => new WalletSummary(w.Id, w.Currency, w.Balance)).ToList();
    }
}
