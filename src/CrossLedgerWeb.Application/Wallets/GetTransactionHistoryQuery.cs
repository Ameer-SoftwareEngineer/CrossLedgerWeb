using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed record GetTransactionHistoryQuery(
    WalletId WalletId,
    UserId CallerId,
    int PageNumber,
    int PageSize,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate) : IRequest<TransactionHistoryPage>;
