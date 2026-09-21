using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

public sealed class OtcRequestNotFoundException : Exception
{
    public OtcRequestId OtcRequestId { get; }

    public OtcRequestNotFoundException(OtcRequestId otcRequestId)
        : base($"OTC request {otcRequestId} was not found.")
    {
        OtcRequestId = otcRequestId;
    }
}
