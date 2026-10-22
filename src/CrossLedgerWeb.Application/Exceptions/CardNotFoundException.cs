using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

public sealed class CardNotFoundException : Exception
{
    public CardId CardId { get; }

    public CardNotFoundException(CardId cardId)
        : base($"Card {cardId} was not found.")
    {
        CardId = cardId;
    }
}
