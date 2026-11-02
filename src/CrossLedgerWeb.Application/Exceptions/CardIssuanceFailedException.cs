namespace CrossLedgerWeb.Application.Exceptions;

/// <summary>Wraps a failure from the configured ICardIssuer (e.g. a Stripe API error) -
/// callers see a stable, typed exception regardless of which issuer is active, the same
/// reason ExchangeRateProviderRejectedException exists for FX providers.</summary>
public sealed class CardIssuanceFailedException : Exception
{
    public CardIssuanceFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
