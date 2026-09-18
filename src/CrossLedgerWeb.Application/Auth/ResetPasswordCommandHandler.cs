using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityService _identity;

    public ResetPasswordCommandHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var succeeded = await _identity.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
        if (!succeeded)
            throw new InvalidPasswordResetException();
    }
}
