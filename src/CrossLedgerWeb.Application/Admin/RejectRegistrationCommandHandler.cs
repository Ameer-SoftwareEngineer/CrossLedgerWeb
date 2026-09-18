using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class RejectRegistrationCommandHandler : IRequestHandler<RejectRegistrationCommand>
{
    private readonly IUserAdminService _userAdmin;

    public RejectRegistrationCommandHandler(IUserAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    public async Task Handle(RejectRegistrationCommand request, CancellationToken cancellationToken)
    {
        var found = await _userAdmin.RejectRegistrationAsync(request.UserId, cancellationToken);
        if (!found)
            throw new UserNotFoundException(request.UserId);
    }
}
