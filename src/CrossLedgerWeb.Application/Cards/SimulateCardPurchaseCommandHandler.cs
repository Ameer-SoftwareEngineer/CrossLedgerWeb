using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Simulates a card purchase - no real card network or merchant acquirer sits
/// behind this. Posts the same shape of ledger entries a real one would (debit the
/// cardholder's wallet, credit the platform's merchant settlement account for that
/// currency), so the purchase shows up in Transaction History exactly like any other
/// debit, and enforces the same card-state/spending-limit checks the entity itself
/// owns.</summary>
public sealed class SimulateCardPurchaseCommandHandler : IRequestHandler<SimulateCardPurchaseCommand, SimulateCardPurchaseResult>
{
    private readonly ICardRepository _cards;
    private readonly ICardPurchaseRepository _purchases;
    private readonly IWalletRepository _wallets;
    private readonly IMerchantSettlementWalletResolver _merchantSettlementWallets;
    private readonly IClock _clock;

    public SimulateCardPurchaseCommandHandler(
        ICardRepository cards,
        ICardPurchaseRepository purchases,
        IWalletRepository wallets,
        IMerchantSettlementWalletResolver merchantSettlementWallets,
        IClock clock)
    {
        _cards = cards;
        _purchases = purchases;
        _wallets = wallets;
        _merchantSettlementWallets = merchantSettlementWallets;
        _clock = clock;
    }

    public async Task<SimulateCardPurchaseResult> Handle(SimulateCardPurchaseCommand request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null || card.OwnerId != request.CallerId)
            throw new CardNotFoundException(request.CardId);

        var amount = new Money(request.Amount, card.Currency);
        card.EnsureCanSpend(amount);

        var wallet = await _wallets.GetByIdAsync(card.CardWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(card.CardWalletId);

        var merchantSettlementWallet = await _merchantSettlementWallets.GetMerchantSettlementWalletAsync(card.Currency, cancellationToken);

        var now = _clock.UtcNow;
        var transferId = TransferId.New();

        CardPurchasePoster.Post(transferId, wallet, merchantSettlementWallet, amount, now);

        var purchase = new CardPurchase(CardPurchaseId.New(), card.Id, transferId, request.MerchantName, amount, now);
        _purchases.Add(purchase);

        return new SimulateCardPurchaseResult(purchase.Id, transferId, purchase.MerchantName, amount, now);
    }
}
