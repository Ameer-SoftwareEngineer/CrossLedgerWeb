using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Providers.Common;
using Microsoft.Extensions.Options;

namespace CrossLedgerWeb.Providers.Stripe;

/// <summary>
/// Stripe's role in this platform is the funding rail (specification 3.5): card top-up
/// into a wallet, not a cross-border payout route. It still implements IPaymentProvider
/// for registration consistency with the other three - the interface is the contract
/// every provider is added behind - but Supports() always returns false, so the routing
/// engine never selects it for an outbound payout, and Quote/Execute reflect that by
/// throwing rather than pretending to do something this provider doesn't do here. A real
/// top-up flow (PaymentIntents, checkout session, webhook-driven wallet credit) is a
/// separate feature from payout routing and is not built by this interface.
/// </summary>
public sealed class StripeProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<StripeOptions> _options;

    public StripeProvider(HttpClient httpClient, IOptions<StripeOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public ProviderCode Code => ProviderCode.Stripe;

    public bool Supports(Corridor corridor) => false;

    public Task<ProviderQuote> QuoteAsync(PayoutRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Stripe is the funding rail, not a payout route - Supports() always returns false, so the routing engine never calls this.");

    public Task<PayoutResult> ExecuteAsync(PayoutRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Stripe is the funding rail, not a payout route - Supports() always returns false, so the routing engine never calls this.");

    /// <summary>Stripe's real webhook scheme - see StripeSignatureVerifier's own doc
    /// comment for the algorithm itself; this just delegates to it so the Issuing
    /// real-time authorization webhook can reuse the identical logic.</summary>
    public bool VerifySignature(string payload, string signature, string secret) =>
        StripeSignatureVerifier.Verify(payload, signature, secret);

    public async Task<ProviderHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "v1/balance");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Value.SecretKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var status = response.IsSuccessStatusCode ? ProviderHealthStatus.Healthy : ProviderHealthStatus.Degraded;
            return new ProviderHealth(Code, status, DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException)
        {
            return new ProviderHealth(Code, ProviderHealthStatus.Unavailable, DateTimeOffset.UtcNow);
        }
    }
}
