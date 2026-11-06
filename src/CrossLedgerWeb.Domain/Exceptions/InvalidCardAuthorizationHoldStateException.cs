using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Exceptions;

/// <summary>Thrown when a capture or release is attempted against a hold that isn't Open -
/// e.g. a redelivered capture webhook racing the expiry sweeper, or a capture arriving after
/// the hold was already captured by an earlier delivery. Application-layer handlers treat
/// this as "already resolved, nothing to do" rather than letting it propagate as an error -
/// same defensive-guard role InvalidCardStateException plays for Card's own transitions.</summary>
public sealed class InvalidCardAuthorizationHoldStateException : DomainException
{
    public CardAuthorizationId AuthorizationId { get; }
    public CardAuthorizationHoldStatus ActualStatus { get; }
    public string AttemptedAction { get; }

    public InvalidCardAuthorizationHoldStateException(CardAuthorizationId authorizationId, CardAuthorizationHoldStatus actualStatus, string attemptedAction)
        : base($"Cannot {attemptedAction} card authorization {authorizationId} - its hold is {actualStatus}.")
    {
        AuthorizationId = authorizationId;
        ActualStatus = actualStatus;
        AttemptedAction = attemptedAction;
    }
}
