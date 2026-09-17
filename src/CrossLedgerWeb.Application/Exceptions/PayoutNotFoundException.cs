using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

public sealed class PayoutNotFoundException : Exception
{
    public ProviderCode? ProviderCode { get; }
    public string? ProviderReference { get; }
    public PayoutId? PayoutId { get; }

    public PayoutNotFoundException(ProviderCode providerCode, string providerReference)
        : base($"No payout found for {providerCode} reference '{providerReference}'.")
    {
        ProviderCode = providerCode;
        ProviderReference = providerReference;
    }

    public PayoutNotFoundException(PayoutId payoutId)
        : base($"Payout {payoutId} was not found.")
    {
        PayoutId = payoutId;
    }
}
