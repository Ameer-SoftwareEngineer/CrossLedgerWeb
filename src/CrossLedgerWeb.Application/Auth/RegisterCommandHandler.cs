using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityService _identity;

    public RegisterCommandHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var details = new RegistrationDetails(
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Address,
            request.PermanentAddress,
            request.City,
            request.StateProvince,
            request.Country,
            request.ProofOfAddressDocumentType,
            request.ProofOfAddressFileName,
            request.ProofOfAddressContentType,
            request.ProofOfAddressContent);

        var outcome = await _identity.RegisterAsync(details, cancellationToken);

        if (!outcome.Succeeded)
            throw new RegistrationFailedException(outcome.Errors);

        return new RegisterResult(outcome.UserId!.Value, request.Email);
    }
}
