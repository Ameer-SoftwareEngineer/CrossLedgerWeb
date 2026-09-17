using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Wallets;

public sealed record ListMyWalletsQuery(UserId OwnerId) : IRequest<IReadOnlyList<WalletSummary>>;
