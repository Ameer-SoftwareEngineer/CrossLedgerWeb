using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserSummary>>
{
    private readonly IUserAdminService _userAdmin;

    public ListUsersQueryHandler(IUserAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    public Task<IReadOnlyList<UserSummary>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        _userAdmin.ListUsersAsync(cancellationToken);
}
