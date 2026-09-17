using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Exceptions;

public sealed class UserNotFoundException : Exception
{
    public UserId UserId { get; }

    public UserNotFoundException(UserId userId)
        : base($"User {userId} was not found.")
    {
        UserId = userId;
    }
}
