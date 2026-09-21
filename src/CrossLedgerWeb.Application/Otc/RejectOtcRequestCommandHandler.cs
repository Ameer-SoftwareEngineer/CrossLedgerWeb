using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

public sealed class RejectOtcRequestCommandHandler : IRequestHandler<RejectOtcRequestCommand>
{
    private readonly IOtcRequestRepository _otcRequests;

    public RejectOtcRequestCommandHandler(IOtcRequestRepository otcRequests)
    {
        _otcRequests = otcRequests;
    }

    public async Task Handle(RejectOtcRequestCommand request, CancellationToken cancellationToken)
    {
        var otcRequest = await _otcRequests.GetByIdAsync(request.OtcRequestId, cancellationToken)
            ?? throw new OtcRequestNotFoundException(request.OtcRequestId);

        if (otcRequest.RequestedByUserId != request.CallerId)
            throw new OtcRequestNotFoundException(request.OtcRequestId);

        otcRequest.Reject();
    }
}
