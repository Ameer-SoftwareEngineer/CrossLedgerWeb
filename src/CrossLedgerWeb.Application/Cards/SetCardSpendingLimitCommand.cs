using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Limit is per-purchase, not a rolling daily/monthly cap - null clears it.</summary>
public sealed record SetCardSpendingLimitCommand(CardId CardId, UserId CallerId, decimal? Limit) : IRequest;
