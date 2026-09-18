namespace CrossLedgerWeb.Application.Abstractions;

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken);
}
