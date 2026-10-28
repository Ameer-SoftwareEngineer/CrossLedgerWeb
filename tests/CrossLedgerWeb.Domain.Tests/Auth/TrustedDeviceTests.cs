using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;

namespace CrossLedgerWeb.Domain.Tests.Auth;

public class TrustedDeviceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static TrustedDevice NewDevice(TimeSpan? validFor = null) => new(
        TrustedDeviceId.New(), UserId.New(), "hash", "Chrome on Windows", Now, validFor ?? TimeSpan.FromDays(30));

    [Fact]
    public void A_freshly_issued_grant_is_active()
    {
        var device = NewDevice();

        device.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void A_grant_is_expired_once_its_validity_window_has_passed()
    {
        var device = NewDevice(TimeSpan.FromDays(30));

        device.IsExpired(Now.AddDays(31)).Should().BeTrue();
        device.IsActive(Now.AddDays(31)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_marks_the_grant_inactive()
    {
        var device = NewDevice();

        device.Revoke(Now);

        device.IsRevoked.Should().BeTrue();
        device.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Revoke_is_idempotent_and_keeps_the_first_revocation()
    {
        var device = NewDevice();

        device.Revoke(Now);
        device.Revoke(Now.AddMinutes(5));

        device.RevokedAt.Should().Be(Now);
    }

    [Fact]
    public void MarkUsed_records_the_last_used_timestamp()
    {
        var device = NewDevice();

        device.MarkUsed(Now.AddDays(2));

        device.LastUsedAt.Should().Be(Now.AddDays(2));
    }

    [Fact]
    public void Constructor_rejects_a_non_positive_validity_window()
    {
        var act = () => new TrustedDevice(TrustedDeviceId.New(), UserId.New(), "hash", "Chrome on Windows", Now, TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_rejects_a_blank_token_hash()
    {
        var act = () => new TrustedDevice(TrustedDeviceId.New(), UserId.New(), "  ", "Chrome on Windows", Now, TimeSpan.FromDays(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_rejects_a_blank_device_label()
    {
        var act = () => new TrustedDevice(TrustedDeviceId.New(), UserId.New(), "hash", "  ", Now, TimeSpan.FromDays(1));

        act.Should().Throw<ArgumentException>();
    }
}
