using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Moves money from one of the owner's own wallets into their card's dedicated
/// wallet - the only way a prepaid card's balance ever increases. Posts through
/// CardTopUpPoster rather than TransferPoster: both wallets are the same currency and
/// both belong to the same owner, so there's no FX leg and no settlement account
/// involved, unlike a cross-currency transfer or a card purchase.</summary>
public sealed class TopUpCardCommandHandler : IRequestHandler<TopUpCardCommand, TopUpCardResult>
{
    private readonly ICardRepository _cards;
    private readonly IWalletRepository _wallets;
    private readonly IClock _clock;

    public TopUpCardCommandHandler(ICardRepository cards, IWalletRepository wallets, IClock clock)
    {
        _cards = cards;
        _wallets = wallets;
        _clock = clock;
    }

    public async Task<TopUpCardResult> Handle(TopUpCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        var cardWallet = await _wallets.GetByIdAsync(card.CardWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(card.CardWalletId);

        var sourceWallet = await _wallets.GetByIdAsync(request.SourceWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.SourceWalletId);

        var amount = new Money(request.Amount, card.Currency);
        var now = _clock.UtcNow;
        var transferId = TransferId.New();

        CardTopUpPoster.Post(transferId, sourceWallet, cardWallet, amount, now);

        return new TopUpCardResult(transferId, amount, cardWallet.Balance.Amount, now);
    }
}
