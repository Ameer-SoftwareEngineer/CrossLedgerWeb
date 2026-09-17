using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Payments;

public sealed record GetRoutingDecisionQuery(PayoutId PayoutId, UserId CallerId) : IRequest<IReadOnlyList<RoutingDecisionEntry>>;
