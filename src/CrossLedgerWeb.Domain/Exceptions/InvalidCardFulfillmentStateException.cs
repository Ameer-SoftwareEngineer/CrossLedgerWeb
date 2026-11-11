using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Exceptions;

/// <summary>Thrown when a shipping-lifecycle action (ship, deliver, return, fail,
/// activate) is attempted against a fulfillment status that doesn't permit it - same
/// shape as InvalidCardStateException, for the separate FulfillmentStatus state
/// machine.</summary>
public sealed class InvalidCardFulfillmentStateException : DomainException
{
    public CardId CardId { get; }
    public CardFulfillmentStatus ActualStatus { get; }
    public string AttemptedAction { get; }

    public InvalidCardFulfillmentStateException(CardId cardId, CardFulfillmentStatus actualStatus, string attemptedAction)
        : base($"Cannot {attemptedAction} card {cardId} - its fulfillment status is {actualStatus}.")
    {
        CardId = cardId;
        ActualStatus = actualStatus;
        AttemptedAction = attemptedAction;
    }
}
