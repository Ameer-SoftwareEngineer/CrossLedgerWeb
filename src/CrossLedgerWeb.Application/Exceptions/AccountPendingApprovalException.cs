namespace CrossLedgerWeb.Application.Exceptions;

public sealed class AccountPendingApprovalException : Exception
{
    public AccountPendingApprovalException()
        : base("This account is still awaiting admin approval.")
    {
    }
}
