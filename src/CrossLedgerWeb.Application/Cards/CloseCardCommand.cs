using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record CloseCardCommand(CardId CardId, UserId CallerId) : IRequest;
