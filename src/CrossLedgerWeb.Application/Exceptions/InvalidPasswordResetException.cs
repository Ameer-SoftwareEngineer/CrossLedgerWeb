namespace CrossLedgerWeb.Application.Exceptions;

public sealed class InvalidPasswordResetException : Exception
{
    public InvalidPasswordResetException()
        : base("This password reset link is invalid or has expired.")
    {
    }
}
