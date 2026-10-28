using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class RevokeTrustedDeviceCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();

    private readonly Mock<ITrustedDeviceRepository> _devices = new();
    private readonly Mock<ISecurityAuditLogRepository> _auditLog = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IClock> _clock = new();

    public RevokeTrustedDeviceCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private RevokeTrustedDeviceCommandHandler CreateHandler() =>
        new(_devices.Object, _auditLog.Object, _requestContext.Object, _clock.Object);

    [Fact]
    public async Task Handle_revokes_a_device_owned_by_the_caller()
    {
        var device = new TrustedDevice(TrustedDeviceId.New(), Owner, "hash", "Chrome on Windows", Now.AddDays(-1), TimeSpan.FromDays(30));
        _devices.Setup(x => x.GetByIdAsync(device.Id, It.IsAny<CancellationToken>())).ReturnsAsync(device);
        var handler = CreateHandler();

        await handler.Handle(new RevokeTrustedDeviceCommand(Owner, device.Id), CancellationToken.None);

        device.IsRevoked.Should().BeTrue();
        _auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == Owner && e.EventType == SecurityEventType.TrustedDeviceRevoked && e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_device_does_not_exist()
    {
        _devices.Setup(x => x.GetByIdAsync(It.IsAny<TrustedDeviceId>(), It.IsAny<CancellationToken>())).ReturnsAsync((TrustedDevice?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RevokeTrustedDeviceCommand(Owner, TrustedDeviceId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<TrustedDeviceNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_device_belongs_to_another_user()
    {
        var device = new TrustedDevice(TrustedDeviceId.New(), UserId.New(), "hash", "Chrome on Windows", Now.AddDays(-1), TimeSpan.FromDays(30));
        _devices.Setup(x => x.GetByIdAsync(device.Id, It.IsAny<CancellationToken>())).ReturnsAsync(device);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RevokeTrustedDeviceCommand(Owner, device.Id), CancellationToken.None);

        await act.Should().ThrowAsync<TrustedDeviceNotFoundException>();
        device.IsRevoked.Should().BeFalse();
    }
}
