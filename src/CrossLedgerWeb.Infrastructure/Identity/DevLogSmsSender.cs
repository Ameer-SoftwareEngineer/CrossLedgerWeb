using CrossLedgerWeb.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CrossLedgerWeb.Infrastructure.Identity;

/// <summary>No SMS gateway (Twilio or otherwise) is configured anywhere in this project,
/// so there is nowhere to actually deliver an SMS - this logs the code instead, the same
/// dev-only transparency as DevelopmentAdminSeeder's well-known credentials. Swapping in
/// a real provider later only means implementing ISmsSender differently and changing the
/// DI registration; nothing above this layer would need to change.</summary>
public sealed class DevLogSmsSender : ISmsSender
{
    private readonly ILogger<DevLogSmsSender> _logger;

    public DevLogSmsSender(ILogger<DevLogSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[DEV SMS - no real gateway configured] To {PhoneNumber}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
