using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Wallets;
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

        // Card-kind wallets are a card's own dedicated balance, not something the owner
        // manages directly - they never belong in the Wallets page, the sidebar, or any
        // "pick a wallet" selector built from this query.
        return wallets
            .Where(w => w.Kind == WalletKind.Customer)
            .Select(w => new WalletSummary(w.Id, w.Currency, w.Balance))
            .ToList();
    }
}
