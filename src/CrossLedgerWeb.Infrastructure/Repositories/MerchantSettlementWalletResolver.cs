using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using CrossLedgerWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class MerchantSettlementWalletResolver : IMerchantSettlementWalletResolver
{
    private readonly CrossLedgerWebDbContext _db;

    public MerchantSettlementWalletResolver(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public async Task<Wallet> GetMerchantSettlementWalletAsync(Currency currency, CancellationToken cancellationToken)
    {
        var wallet = await _db.Wallets
            .Include(w => w.Entries)
            .FirstOrDefaultAsync(w => w.Kind == WalletKind.MerchantSettlement && w.Currency == currency, cancellationToken);

        return wallet ?? throw new MerchantSettlementWalletNotConfiguredException(currency);
    }
}
