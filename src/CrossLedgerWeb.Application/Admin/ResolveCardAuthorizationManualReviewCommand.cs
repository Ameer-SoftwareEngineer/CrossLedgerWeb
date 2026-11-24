using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

/// <summary>Admin-only - specification 9's Admin Console, same trust boundary as
/// ApproveCardRequestCommand. A deliberate acknowledgment only - see
/// CardAuthorizationHoldStatus.ManuallyResolved's own doc comment for why this never moves
/// money.</summary>
public sealed record ResolveCardAuthorizationManualReviewCommand(CardAuthorizationId CardAuthorizationId) : IRequest;
