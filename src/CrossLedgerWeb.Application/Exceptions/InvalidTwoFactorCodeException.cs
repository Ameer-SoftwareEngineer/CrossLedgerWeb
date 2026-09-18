namespace CrossLedgerWeb.Application.Exceptions;

public sealed class InvalidTwoFactorCodeException : Exception
{
    public InvalidTwoFactorCodeException()
        : base("That verification code is incorrect.")
    {
    }
}
