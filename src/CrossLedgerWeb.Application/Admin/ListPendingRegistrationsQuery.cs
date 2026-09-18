using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListPendingRegistrationsQuery : IRequest<IReadOnlyList<PendingRegistration>>;
