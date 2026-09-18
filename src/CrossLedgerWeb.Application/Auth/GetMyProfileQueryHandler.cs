using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfile?>
{
    private readonly IIdentityService _identity;

    public GetMyProfileQueryHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public Task<MyProfile?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken) =>
        _identity.GetMyProfileAsync(request.UserId, cancellationToken);
}
