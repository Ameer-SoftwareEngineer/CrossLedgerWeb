using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Exceptions;

/// <summary>Thrown when a card lifecycle action (freeze, unfreeze, close, spend) is
/// attempted against a status that doesn't permit it - e.g. unfreezing a card that was
/// never frozen, or spending on a closed one.</summary>
public sealed class InvalidCardStateException : DomainException
{
    public CardId CardId { get; }
    public CardStatus ActualStatus { get; }
    public string AttemptedAction { get; }

    public InvalidCardStateException(CardId cardId, CardStatus actualStatus, string attemptedAction)
        : base($"Cannot {attemptedAction} card {cardId} - it is {actualStatus}.")
    {
        CardId = cardId;
        ActualStatus = actualStatus;
        AttemptedAction = attemptedAction;
    }
}
