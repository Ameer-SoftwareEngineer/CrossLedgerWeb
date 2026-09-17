using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using MediatR;

namespace CrossLedgerWeb.Application.Ledger;

public sealed class GetTransferLedgerEntriesQueryHandler : IRequestHandler<GetTransferLedgerEntriesQuery, IReadOnlyList<LedgerEntryDetail>>
{
    private readonly ILedgerEntryReader _reader;
    private readonly IWalletRepository _wallets;

    public GetTransferLedgerEntriesQueryHandler(ILedgerEntryReader reader, IWalletRepository wallets)
    {
        _reader = reader;
        _wallets = wallets;
    }

    public async Task<IReadOnlyList<LedgerEntryDetail>> Handle(GetTransferLedgerEntriesQuery request, CancellationToken cancellationToken)
    {
        var entries = await _reader.GetByTransferIdAsync(request.TransferId, cancellationToken);
        if (entries.Count == 0)
            throw new TransferNotFoundException(request.TransferId);

        // A transfer touches up to four wallets, but FX settlement wallets belong to the
        // platform, not a customer - the caller only needs to own one of them (the source
        // or target leg) to be entitled to see the whole breakdown, same 404-either-way
        // rule as the transaction history endpoint so this can't be used to probe ids.
        var isParty = false;
        foreach (var walletId in entries.Select(e => e.WalletId).Distinct())
        {
            var ownerId = await _wallets.GetOwnerIdAsync(walletId, cancellationToken);
            if (ownerId == request.CallerId)
            {
                isParty = true;
                break;
            }
        }

        if (!isParty)
            throw new TransferNotFoundException(request.TransferId);

        return entries;
    }
}
