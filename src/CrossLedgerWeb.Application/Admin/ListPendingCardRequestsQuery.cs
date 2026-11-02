using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListPendingCardRequestsQuery : IRequest<IReadOnlyList<PendingCardRequest>>;
