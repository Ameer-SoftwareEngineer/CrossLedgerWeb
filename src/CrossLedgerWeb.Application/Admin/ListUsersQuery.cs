using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListUsersQuery : IRequest<IReadOnlyList<UserSummary>>;
