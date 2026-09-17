namespace CrossLedgerWeb.Shared.Wallets;

public sealed record WalletSummaryResponse(Guid WalletId, string Currency, decimal Balance);
