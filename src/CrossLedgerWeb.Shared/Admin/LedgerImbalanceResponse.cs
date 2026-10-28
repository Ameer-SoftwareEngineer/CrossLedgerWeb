namespace CrossLedgerWeb.Shared.Admin;

public sealed record LedgerImbalanceResponse(string CurrencyCode, decimal NetAmount);
