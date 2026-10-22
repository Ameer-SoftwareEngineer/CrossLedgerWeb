using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Exceptions;

public sealed class CardSpendingLimitExceededException : DomainException
{
    public CardId CardId { get; }
    public decimal AttemptedAmount { get; }
    public decimal SpendingLimit { get; }

    public CardSpendingLimitExceededException(CardId cardId, decimal attemptedAmount, decimal spendingLimit)
        : base($"Card {cardId}'s per-purchase limit is {spendingLimit}; attempted {attemptedAmount}.")
    {
        CardId = cardId;
        AttemptedAmount = attemptedAmount;
        SpendingLimit = spendingLimit;
    }
}
