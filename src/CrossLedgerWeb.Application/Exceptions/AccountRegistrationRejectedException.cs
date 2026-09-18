namespace CrossLedgerWeb.Application.Exceptions;

public sealed class AccountRegistrationRejectedException : Exception
{
    public AccountRegistrationRejectedException()
        : base("This account's registration was not approved.")
    {
    }
}
