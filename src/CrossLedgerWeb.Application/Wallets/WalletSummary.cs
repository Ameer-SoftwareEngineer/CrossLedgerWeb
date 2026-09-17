using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Wallets;

public sealed record WalletSummary(WalletId WalletId, Currency Currency, Money Balance);
