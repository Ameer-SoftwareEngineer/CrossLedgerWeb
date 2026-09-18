namespace CrossLedgerWeb.Application.Exceptions;

public sealed class InvalidTwoFactorChallengeException : Exception
{
    public InvalidTwoFactorChallengeException()
        : base("This verification session is invalid or has expired. Please log in again.")
    {
    }
}
