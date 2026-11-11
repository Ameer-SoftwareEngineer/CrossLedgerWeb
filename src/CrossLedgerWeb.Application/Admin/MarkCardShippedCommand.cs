using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

/// <summary>Admin-only - specification 9's Admin Console, same trust boundary as
/// ApproveCardRequestCommand.</summary>
public sealed record MarkCardShippedCommand(CardId CardId, string Carrier, string TrackingNumber) : IRequest;
