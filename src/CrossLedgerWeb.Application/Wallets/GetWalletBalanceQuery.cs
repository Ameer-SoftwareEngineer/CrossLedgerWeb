using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed record GetWalletBalanceQuery(WalletId WalletId, UserId CallerId) : IRequest<Money>;
