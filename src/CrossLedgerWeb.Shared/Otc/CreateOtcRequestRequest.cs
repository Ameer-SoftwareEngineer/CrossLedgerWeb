namespace CrossLedgerWeb.Shared.Otc;

/// <summary>Wire contract for POST /api/v1/otc/requests.</summary>
public sealed record CreateOtcRequestRequest(Guid SourceWalletId, string FromCurrency, string ToCurrency, decimal Amount);
