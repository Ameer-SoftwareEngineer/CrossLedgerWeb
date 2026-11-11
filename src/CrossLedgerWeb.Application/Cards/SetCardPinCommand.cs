using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record SetCardPinCommand(CardId CardId, UserId CallerId, string Pin) : IRequest;
