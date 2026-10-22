using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record SimulateCardPurchaseCommand(
    CardId CardId,
    UserId CallerId,
    string MerchantName,
    decimal Amount,
    string IdempotencyKey) : IRequest<SimulateCardPurchaseResult>, IIdempotentRequest;
