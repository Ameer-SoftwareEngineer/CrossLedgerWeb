using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

public sealed record ListMyOtcRequestsQuery(UserId RequestedByUserId) : IRequest<IReadOnlyList<OtcRequestSummary>>;
