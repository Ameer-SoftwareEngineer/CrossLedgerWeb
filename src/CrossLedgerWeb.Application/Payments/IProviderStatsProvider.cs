using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Payments;

public interface IProviderStatsProvider
{
    /// <summary>currency scopes the daily-volume-headroom signal to the corridor actually
    /// being routed - specification 5.3's "remaining daily volume allowance" is naturally a
    /// single-currency figure, same reasoning as ITransferLimitsReader's own per-currency
    /// scoping, and avoids needing an FX conversion to compare volume across currencies.</summary>
    Task<ProviderStats> GetStatsAsync(ProviderCode providerCode, Currency currency, CancellationToken cancellationToken);
}
