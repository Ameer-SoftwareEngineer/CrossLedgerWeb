using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Infrastructure.Persistence;
using CrossLedgerWeb.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CrossLedgerWeb.Infrastructure.Webhooks;

/// <summary>Implements specification 2.6's outbound webhook delivery: HMAC-SHA256 signs
/// the payload (same algorithm CrossLedgerWeb.Providers.Common.HmacSignatureVerifier
/// verifies inbound provider webhooks with - inlined rather than referenced, since
/// Infrastructure and Providers are kept as parallel siblings everywhere else in this
/// codebase, only composed together at the Api layer), retries transient failures with
/// exponential backoff - the same ResiliencePipeline shape as
/// ResilientExchangeRateProvider, just retry-only since there is no second subscriber to
/// fall back to - and records every attempt via CrossLedgerWebDbContext directly,
/// committed by UnitOfWorkBehavior same as ProcessedWebhookEventStore.MarkProcessed.</summary>
public sealed class WebhookNotifier : IWebhookNotifier
{
    private readonly HttpClient _httpClient;
    private readonly CrossLedgerWebDbContext _db;
    private readonly WebhookOptions _options;
    private readonly ILogger<WebhookNotifier> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public WebhookNotifier(HttpClient httpClient, CrossLedgerWebDbContext db, IOptions<WebhookOptions> options, ILogger<WebhookNotifier> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _options = options.Value;
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
            })
            .Build();
    }

    public async Task NotifyAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SubscriberUrl))
            return;

        var json = JsonSerializer.Serialize(payload);
        var signature = Sign(json, _options.SubscriberSecret);

        var attempts = 0;
        var succeeded = false;
        int? statusCode = null;

        try
        {
            using var response = await _pipeline.ExecuteAsync(async token =>
            {
                attempts++;
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.SubscriberUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                request.Headers.Add("X-CrossLedger-Signature", signature);
                request.Headers.Add("X-CrossLedger-Event", eventType);

                return await _httpClient.SendAsync(request, token);
            }, cancellationToken);

            statusCode = (int)response.StatusCode;
            succeeded = response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Every retry the pipeline made failed (connection refused, DNS failure, or
            // retries exhausted against a non-2xx response) - recorded as a failed
            // delivery below rather than propagating and failing whatever triggered this
            // notification. A subscriber being unreachable is the subscriber's problem,
            // not a reason to fail the payout webhook that triggered it.
            _logger.LogWarning(ex, "Webhook delivery of {EventType} to the configured subscriber failed after {Attempts} attempt(s).", eventType, attempts);
        }

        _db.WebhookDeliveries.Add(new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = json,
            SubscriberUrl = _options.SubscriberUrl,
            Succeeded = succeeded,
            ResponseStatusCode = statusCode,
            AttemptCount = attempts,
            AttemptedAt = DateTimeOffset.UtcNow,
        });
    }

    public async Task<WebhookDeliveryPage> ListRecentAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.WebhookDeliveries.OrderByDescending(d => d.AttemptedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new WebhookDeliverySummary(d.EventType, d.SubscriberUrl, d.Succeeded, d.ResponseStatusCode, d.AttemptCount, d.AttemptedAt))
            .ToListAsync(cancellationToken);

        return new WebhookDeliveryPage(page, totalCount, pageNumber, pageSize);
    }

    private static string Sign(string payload, string secret) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
}
