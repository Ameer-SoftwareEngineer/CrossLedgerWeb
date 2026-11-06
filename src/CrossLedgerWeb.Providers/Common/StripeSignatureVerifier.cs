namespace CrossLedgerWeb.Providers.Common;

/// <summary>
/// Stripe's real webhook scheme - a Stripe-Signature header shaped
/// "t=&lt;unix timestamp&gt;,v1=&lt;hex hmac&gt;" over "{timestamp}.{payload}", not a bare
/// signature over the raw payload like the other providers' webhooks. Implemented against
/// Stripe's actual, stable, publicly documented algorithm - doesn't need a live sandbox to
/// get right, unlike most of this codebase's Stripe integration. Extracted out of
/// StripeProvider.VerifySignature (which still delegates here) so the Issuing real-time
/// authorization webhook - a different endpoint, a different secret
/// (Stripe:IssuingWebhookSecret, not the payout-webhook Stripe:WebhookSecret IPaymentProvider
/// resolves) - can use the identical, already-correct parsing without duplicating it or
/// going through the unrelated payout-provider interface to get it.
/// </summary>
public static class StripeSignatureVerifier
{
    public static bool Verify(string payload, string signatureHeader, string secret)
    {
        var parts = signatureHeader
            .Split(',')
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1]);

        if (!parts.TryGetValue("t", out var timestamp) || !parts.TryGetValue("v1", out var providedSignature))
            return false;

        var signedPayload = $"{timestamp}.{payload}";
        return HmacSignatureVerifier.Verify(signedPayload, providedSignature, secret);
    }
}
