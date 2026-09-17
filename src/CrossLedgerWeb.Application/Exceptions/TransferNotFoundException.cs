using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

public sealed class TransferNotFoundException : Exception
{
    public TransferId TransferId { get; }

    public TransferNotFoundException(TransferId transferId)
        : base($"Transfer {transferId} was not found.")
    {
        TransferId = transferId;
    }
}
