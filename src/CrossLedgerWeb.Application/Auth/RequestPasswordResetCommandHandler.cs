using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
{
    private readonly IIdentityService _identity;

    public RequestPasswordResetCommandHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken) =>
        _identity.RequestPasswordResetAsync(request.Email, cancellationToken);
}
