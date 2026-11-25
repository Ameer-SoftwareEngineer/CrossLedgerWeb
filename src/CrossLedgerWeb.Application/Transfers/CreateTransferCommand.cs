using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Transfers;

public sealed record CreateTransferCommand(
    QuoteId QuoteId,
    WalletId SourceWalletId,
    WalletId TargetWalletId,
    decimal SourceAmount,
    UserId CallerId,
    string IdempotencyKey) : IRequest<CreateTransferResult>, IIdempotentRequest;
