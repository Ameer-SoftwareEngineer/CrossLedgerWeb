using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record TopUpCardCommand(
    CardId CardId,
    UserId CallerId,
    WalletId SourceWalletId,
    decimal Amount,
    string IdempotencyKey) : IRequest<TopUpCardResult>, IIdempotentRequest;
