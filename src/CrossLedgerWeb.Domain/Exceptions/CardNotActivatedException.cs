using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Exceptions;

/// <summary>Thrown by Card.EnsureCanSpend for a physical card that hasn't been activated
/// yet - distinct from InvalidCardStateException so DecideCardAuthorizationCommandHandler
/// can report a precise decline reason ("card_not_activated") rather than the generic
/// "card_not_active".</summary>
public sealed class CardNotActivatedException : DomainException
{
    public CardId CardId { get; }

    public CardNotActivatedException(CardId cardId)
        : base($"Card {cardId} has not been activated yet.")
    {
        CardId = cardId;
    }
}
