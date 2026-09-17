using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Payments;

public sealed class GetRoutingDecisionQueryHandler : IRequestHandler<GetRoutingDecisionQuery, IReadOnlyList<RoutingDecisionEntry>>
{
    private readonly IPayoutRepository _payouts;
    private readonly IWalletRepository _wallets;
    private readonly IRoutingAuditLog _auditLog;

    public GetRoutingDecisionQueryHandler(IPayoutRepository payouts, IWalletRepository wallets, IRoutingAuditLog auditLog)
    {
        _payouts = payouts;
        _wallets = wallets;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<RoutingDecisionEntry>> Handle(GetRoutingDecisionQuery request, CancellationToken cancellationToken)
    {
        var payout = await _payouts.GetByIdAsync(request.PayoutId, cancellationToken)
            ?? throw new PayoutNotFoundException(request.PayoutId);

        // Same 404-either-way rule as the transaction history and ledger endpoints - a
        // payout that exists but belongs to someone else must look identical to one that
        // doesn't exist at all.
        var ownerId = await _wallets.GetOwnerIdAsync(payout.SourceWalletId, cancellationToken);
        if (ownerId != request.CallerId)
            throw new PayoutNotFoundException(request.PayoutId);

        return await _auditLog.GetByTransferIdAsync(payout.TransferId, cancellationToken);
    }
}
