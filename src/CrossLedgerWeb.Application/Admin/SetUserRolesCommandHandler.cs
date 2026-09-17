using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand>
{
    private readonly IUserAdminService _userAdmin;

    public SetUserRolesCommandHandler(IUserAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    public async Task Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        var found = await _userAdmin.SetRolesAsync(request.UserId, request.Roles, cancellationToken);
        if (!found)
            throw new UserNotFoundException(request.UserId);
    }
}
