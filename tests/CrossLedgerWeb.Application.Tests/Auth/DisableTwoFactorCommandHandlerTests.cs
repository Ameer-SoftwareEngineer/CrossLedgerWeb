using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class DisableTwoFactorCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITwoFactorCredentialRepository> _credentials = new();
    private readonly Mock<ISecurityAuditLogRepository> _auditLog = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IClock> _clock = new();

    public DisableTwoFactorCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private DisableTwoFactorCommandHandler CreateHandler() =>
        new(_credentials.Object, _auditLog.Object, _requestContext.Object, _clock.Object);

    [Fact]
    public async Task Handle_disables_an_existing_credential()
    {
        var credential = new TwoFactorCredential(TwoFactorCredentialId.New(), UserId.New(), "SECRET", DateTimeOffset.UtcNow);
        _credentials.Setup(x => x.GetByUserIdAsync(credential.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        var handler = CreateHandler();

        await handler.Handle(new DisableTwoFactorCommand(credential.UserId), CancellationToken.None);

        credential.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_throws_when_no_credential_exists()
    {
        _credentials.Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ReturnsAsync((TwoFactorCredential?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new DisableTwoFactorCommand(UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<TwoFactorNotEnabledException>();
    }

    [Fact]
    public async Task Handle_records_an_audit_event_when_two_factor_is_disabled()
    {
        var credential = new TwoFactorCredential(TwoFactorCredentialId.New(), UserId.New(), "SECRET", DateTimeOffset.UtcNow);
        _credentials.Setup(x => x.GetByUserIdAsync(credential.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        var handler = CreateHandler();

        await handler.Handle(new DisableTwoFactorCommand(credential.UserId), CancellationToken.None);

        _auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == credential.UserId && e.EventType == SecurityEventType.TwoFactorDisabled && e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
