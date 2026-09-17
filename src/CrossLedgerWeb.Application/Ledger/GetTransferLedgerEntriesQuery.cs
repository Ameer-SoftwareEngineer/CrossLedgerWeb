using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Ledger;

public sealed record GetTransferLedgerEntriesQuery(TransferId TransferId, UserId CallerId) : IRequest<IReadOnlyList<LedgerEntryDetail>>;
