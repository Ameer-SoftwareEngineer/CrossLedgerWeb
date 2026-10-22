using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record FreezeCardCommand(CardId CardId, UserId CallerId) : IRequest;
