using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Persistence;
using CrossLedgerWeb.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossLedgerWeb.Infrastructure.Repositories;

public sealed class RoutingAuditLog : IRoutingAuditLog
{
    private readonly CrossLedgerWebDbContext _db;
    private readonly IClock _clock;

    public RoutingAuditLog(CrossLedgerWebDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Only stages the records - like IdempotencyStore, the pipeline's single
    /// UnitOfWorkBehavior SaveChanges call is what actually commits them.</summary>
    public Task RecordAsync(TransferId transferId, IReadOnlyList<ScoredProviderQuote> rankedQuotes, CancellationToken cancellationToken)
    {
        var recordedAt = _clock.UtcNow;

        for (var rank = 0; rank < rankedQuotes.Count; rank++)
        {
            var scored = rankedQuotes[rank];
            _db.RoutingDecisions.Add(new RoutingDecisionRecord
            {
                Id = Guid.NewGuid(),
                TransferId = transferId.Value,
                ProviderCode = scored.Quote.ProviderCode.ToString(),
                Rank = rank,
                Score = scored.Score,
                FeeAmount = scored.Quote.Fee.Amount,
                FeeCurrency = scored.Quote.Fee.Currency.Code,
                EstimatedSettlementMinutes = scored.Quote.EstimatedSettlementTime.TotalMinutes,
                RecordedAt = recordedAt,
            });
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RoutingDecisionEntry>> GetByTransferIdAsync(TransferId transferId, CancellationToken cancellationToken)
    {
        var records = await _db.RoutingDecisions
            .Where(r => r.TransferId == transferId.Value)
            .OrderBy(r => r.Rank)
            .ToListAsync(cancellationToken);

        return records
            .Select(r => new RoutingDecisionEntry(
                r.ProviderCode, r.Rank, r.Score, new Money(r.FeeAmount, Currency.From(r.FeeCurrency)), r.EstimatedSettlementMinutes, r.RecordedAt))
            .ToList();
    }
}
