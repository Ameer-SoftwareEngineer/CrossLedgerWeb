using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Resolves the platform's MerchantSettlement wallet for a currency - where
/// funds land when a simulated card purchase debits a customer's wallet. Mirrors
/// <see cref="IFxSettlementWalletResolver"/>/<see cref="IPayoutReserveWalletResolver"/>'s
/// shape deliberately; kept as its own resolver because it resolves a different
/// WalletKind for a different reason, not because the lookup itself differs.</summary>
public interface IMerchantSettlementWalletResolver
{
    Task<Wallet> GetMerchantSettlementWalletAsync(Currency currency, CancellationToken cancellationToken);
}
