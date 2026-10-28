using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ReconcileLedgerQueryHandler : IRequestHandler<ReconcileLedgerQuery, IReadOnlyList<LedgerImbalance>>
{
    private readonly ILedgerReconciliationReader _reader;

    public ReconcileLedgerQueryHandler(ILedgerReconciliationReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<LedgerImbalance>> Handle(ReconcileLedgerQuery request, CancellationToken cancellationToken) =>
        _reader.ReconcileAsync(cancellationToken);
}
