using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListPendingRegistrationsQueryHandler
    : IRequestHandler<ListPendingRegistrationsQuery, IReadOnlyList<PendingRegistration>>
{
    private readonly IUserAdminService _userAdmin;

    public ListPendingRegistrationsQueryHandler(IUserAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    public Task<IReadOnlyList<PendingRegistration>> Handle(ListPendingRegistrationsQuery request, CancellationToken cancellationToken) =>
        _userAdmin.ListPendingRegistrationsAsync(cancellationToken);
}
