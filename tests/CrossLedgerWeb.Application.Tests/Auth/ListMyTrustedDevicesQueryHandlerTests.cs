using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class ListMyTrustedDevicesQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId User = UserId.New();

    private readonly Mock<ITrustedDeviceRepository> _devices = new();
    private readonly Mock<IClock> _clock = new();

    public ListMyTrustedDevicesQueryHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private ListMyTrustedDevicesQueryHandler CreateHandler() => new(_devices.Object, _clock.Object);

    [Fact]
    public async Task Handle_returns_only_active_devices_newest_use_first()
    {
        var active1 = new TrustedDevice(TrustedDeviceId.New(), User, "hash1", "Chrome on Windows", Now.AddDays(-10), TimeSpan.FromDays(30));
        active1.MarkUsed(Now.AddDays(-1));
        var active2 = new TrustedDevice(TrustedDeviceId.New(), User, "hash2", "Safari on iOS", Now.AddDays(-5), TimeSpan.FromDays(30));
        var expired = new TrustedDevice(TrustedDeviceId.New(), User, "hash3", "Firefox on Linux", Now.AddDays(-40), TimeSpan.FromDays(30));
        var revoked = new TrustedDevice(TrustedDeviceId.New(), User, "hash4", "Edge on Windows", Now.AddDays(-2), TimeSpan.FromDays(30));
        revoked.Revoke(Now);

        _devices.Setup(x => x.GetByUserIdAsync(User, It.IsAny<CancellationToken>()))
            .ReturnsAsync([active1, active2, expired, revoked]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ListMyTrustedDevicesQuery(User), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(d => d.DeviceLabel).Should().BeEquivalentTo(["Chrome on Windows", "Safari on iOS"]);
        result[0].DeviceLabel.Should().Be("Chrome on Windows"); // most recently used first
    }
}
