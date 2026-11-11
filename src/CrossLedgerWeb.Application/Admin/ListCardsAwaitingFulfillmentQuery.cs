using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListCardsAwaitingFulfillmentQuery : IRequest<IReadOnlyList<CardAwaitingFulfillment>>;
