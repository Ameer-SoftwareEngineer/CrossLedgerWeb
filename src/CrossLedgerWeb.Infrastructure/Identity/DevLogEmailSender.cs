using CrossLedgerWeb.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CrossLedgerWeb.Infrastructure.Identity;

/// <summary>No email provider (SendGrid, SES, SMTP, or otherwise) is configured anywhere
/// in this project, so there is nowhere to actually deliver an email - this logs it
/// instead, the same dev-only transparency as DevLogSmsSender and
/// DevelopmentAdminSeeder's well-known credentials. Swapping in a real provider later
/// only means implementing IEmailSender differently and changing the DI registration.</summary>
public sealed class DevLogEmailSender : IEmailSender
{
    private readonly ILogger<DevLogEmailSender> _logger;

    public DevLogEmailSender(ILogger<DevLogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[DEV EMAIL - no real provider configured] To {ToEmail}, Subject: {Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
