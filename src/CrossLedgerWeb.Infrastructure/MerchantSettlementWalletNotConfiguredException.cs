using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Infrastructure;

/// <summary>Thrown when no MerchantSettlement wallet exists for a currency a card
/// purchase is trying to post in. This is an operator error (a currency was enabled for
/// cards without provisioning its settlement account), never a client-facing 404 - it
/// should page operations, not the caller.</summary>
public sealed class MerchantSettlementWalletNotConfiguredException : Exception
{
    public Currency Currency { get; }

    public MerchantSettlementWalletNotConfiguredException(Currency currency)
        : base($"No merchant settlement wallet is configured for {currency.Code}.")
    {
        Currency = currency;
    }
}
