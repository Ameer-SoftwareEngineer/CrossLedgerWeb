using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

/// <summary>Thrown when usp_CheckTransferLimits reports that a transfer would push the
/// caller's rolling 24h debit total (in the transfer's source currency) past the
/// configured limit.</summary>
public sealed class TransferLimitExceededException : Exception
{
    public UserId OwnerId { get; }
    public Currency Currency { get; }
    public decimal ProjectedTotal { get; }
    public decimal RollingLimit { get; }

    public TransferLimitExceededException(UserId ownerId, Currency currency, decimal projectedTotal, decimal rollingLimit)
        : base($"Transfer would bring {ownerId}'s rolling 24h {currency.Code} total to {projectedTotal}, over the {rollingLimit} limit.")
    {
        OwnerId = ownerId;
        Currency = currency;
        ProjectedTotal = projectedTotal;
        RollingLimit = rollingLimit;
    }
}
