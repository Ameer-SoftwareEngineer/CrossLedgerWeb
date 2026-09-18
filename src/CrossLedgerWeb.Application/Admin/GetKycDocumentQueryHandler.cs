using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class GetKycDocumentQueryHandler : IRequestHandler<GetKycDocumentQuery, KycDocument?>
{
    private readonly IUserAdminService _userAdmin;

    public GetKycDocumentQueryHandler(IUserAdminService userAdmin)
    {
        _userAdmin = userAdmin;
    }

    public Task<KycDocument?> Handle(GetKycDocumentQuery request, CancellationToken cancellationToken) =>
        _userAdmin.GetKycDocumentAsync(request.UserId, cancellationToken);
}
