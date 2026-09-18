using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ApproveRegistrationCommandHandler : IRequestHandler<ApproveRegistrationCommand>
{
    private readonly IUserAdminService _userAdmin;

    public ApproveRegistrationCommandHandler(IUserAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    public async Task Handle(ApproveRegistrationCommand request, CancellationToken cancellationToken)
    {
        var found = await _userAdmin.ApproveRegistrationAsync(request.UserId, cancellationToken);
        if (!found)
            throw new UserNotFoundException(request.UserId);
    }
}
