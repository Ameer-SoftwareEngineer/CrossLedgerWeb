using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

public sealed class TrustedDeviceNotFoundException : Exception
{
    public TrustedDeviceId TrustedDeviceId { get; }

    public TrustedDeviceNotFoundException(TrustedDeviceId trustedDeviceId)
        : base($"Trusted device {trustedDeviceId} was not found.")
    {
        TrustedDeviceId = trustedDeviceId;
    }
}
