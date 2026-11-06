using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record ListCardAuthorizationsQuery(CardId CardId, UserId CallerId) : IRequest<IReadOnlyList<CardAuthorizationSummary>>;
