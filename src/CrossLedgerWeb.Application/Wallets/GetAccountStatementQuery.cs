using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed record GetAccountStatementQuery(
    WalletId WalletId, UserId CallerId, DateTimeOffset FromDate, DateTimeOffset ToDate) : IRequest<AccountStatement>;

/// <summary>RunningBalance is the wallet's actual balance immediately after this entry
/// posted - specification 4.1's "running balance via SUM() OVER (ORDER BY PostedAt)",
/// computed in dbo.usp_GetAccountStatement rather than accumulated in C#.</summary>
public sealed record AccountStatementEntry(
    LedgerEntryId Id, TransferId TransferId, LedgerDirection Direction, Money Amount, DateTimeOffset PostedAt, decimal RunningBalance);

public sealed record AccountStatement(Currency Currency, decimal OpeningBalance, IReadOnlyList<AccountStatementEntry> Entries);
