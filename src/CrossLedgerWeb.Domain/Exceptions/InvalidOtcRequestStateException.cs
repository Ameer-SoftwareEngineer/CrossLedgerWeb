using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Exceptions;

/// <summary>Thrown by <see cref="OtcRequest.Accept"/>/<see cref="OtcRequest.Reject"/>
/// when the request isn't still Offered - stops the same desk offer from being accepted
/// (or accepted then rejected) twice.</summary>
public sealed class InvalidOtcRequestStateException : DomainException
{
    public OtcRequestId OtcRequestId { get; }
    public OtcRequestStatus ActualStatus { get; }

    public InvalidOtcRequestStateException(OtcRequestId otcRequestId, OtcRequestStatus actualStatus)
        : base($"OTC request {otcRequestId} is {actualStatus}, not Offered.")
    {
        OtcRequestId = otcRequestId;
        ActualStatus = actualStatus;
    }
}
