using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

public sealed class CardAuthorizationNotFoundException : Exception
{
    public CardAuthorizationId CardAuthorizationId { get; }

    public CardAuthorizationNotFoundException(CardAuthorizationId cardAuthorizationId)
        : base($"Card authorization {cardAuthorizationId} was not found.")
    {
        CardAuthorizationId = cardAuthorizationId;
    }
}
