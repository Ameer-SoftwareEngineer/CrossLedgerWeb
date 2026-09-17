using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Infrastructure.Persistence;
using CrossLedgerWeb.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class ProcessedWebhookEventStore : IProcessedWebhookEventStore
{
    private readonly CrossLedgerWebDbContext _db;

    public ProcessedWebhookEventStore(CrossLedgerWebDbContext db)
    {
        _db = db;
    }

    public Task<bool> HasBeenProcessedAsync(ProviderCode providerCode, string eventId, CancellationToken cancellationToken) =>
        _db.ProcessedWebhookEvents.AnyAsync(
            e => e.ProviderCode == providerCode.ToString() && e.EventId == eventId,
            cancellationToken);

    public void MarkProcessed(ProviderCode providerCode, string eventId, DateTimeOffset processedAt) =>
        _db.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            ProviderCode = providerCode.ToString(),
            EventId = eventId,
            ProcessedAt = processedAt,
        });

    public async Task<WebhookEventPage> ListRecentAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.ProcessedWebhookEvents.OrderByDescending(e => e.ProcessedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new WebhookEventSummary(e.ProviderCode, e.EventId, e.ProcessedAt))
            .ToListAsync(cancellationToken);

        return new WebhookEventPage(page, totalCount, pageNumber, pageSize);
    }
}
