using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record ListMyCardsQuery(UserId OwnerId) : IRequest<IReadOnlyList<CardSummary>>;
