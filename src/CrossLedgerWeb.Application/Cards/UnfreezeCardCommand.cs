using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record UnfreezeCardCommand(CardId CardId, UserId CallerId) : IRequest;
