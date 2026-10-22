using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record ListCardPurchasesQuery(CardId CardId, UserId CallerId) : IRequest<IReadOnlyList<CardPurchaseSummary>>;
