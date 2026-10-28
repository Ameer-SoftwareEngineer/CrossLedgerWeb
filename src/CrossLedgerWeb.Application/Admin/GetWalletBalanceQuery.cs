using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

/// <summary>Specification 4.1's usp_GetWalletBalance, exposed as an admin diagnostic -
/// a raw-SQL cross-check against the EF-derived Wallet.Balance.</summary>
public sealed record GetWalletBalanceQuery(WalletId WalletId) : IRequest<WalletBalanceResult>;

public sealed record WalletBalanceResult(Currency Currency, decimal Balance);
