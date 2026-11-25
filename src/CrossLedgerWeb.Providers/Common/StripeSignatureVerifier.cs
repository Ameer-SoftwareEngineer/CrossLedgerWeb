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
    /// <summary>Stripe's own recommended tolerance - a captured, genuinely-valid signed
    /// payload is otherwise replayable indefinitely, since the HMAC alone never expires.
    /// Every real caller of this (webhook deduplication by event/authorization/transaction
    /// id) already makes replay a safe no-op, so this is defense-in-depth, not closing a
    /// live exploit - but a timestamp with no freshness check at all is a real gap on its
    /// own regardless of what else happens to mitigate it.</summary>
    private static readonly TimeSpan ToleranceWindow = TimeSpan.FromMinutes(5);

    public static bool Verify(string payload, string signatureHeader, string secret)
    {
        var parts = signatureHeader
            .Split(',')
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1]);

        if (!parts.TryGetValue("t", out var timestamp) || !parts.TryGetValue("v1", out var providedSignature))
            return false;

        if (!long.TryParse(timestamp, out var unixSeconds))
            return false;

        var signedAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var age = DateTimeOffset.UtcNow - signedAt;
        if (age < TimeSpan.Zero || age > ToleranceWindow)
            return false;

        var signedPayload = $"{timestamp}.{payload}";
        return HmacSignatureVerifier.Verify(signedPayload, providedSignature, secret);
    }
}
