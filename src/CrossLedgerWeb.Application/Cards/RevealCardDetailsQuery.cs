using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record RevealCardDetailsQuery(CardId CardId, UserId CallerId) : IRequest<RevealCardDetailsResult>;
