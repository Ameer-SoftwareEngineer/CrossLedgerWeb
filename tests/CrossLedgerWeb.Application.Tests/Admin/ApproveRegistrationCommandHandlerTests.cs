using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ApproveRegistrationCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId AdminUserId = UserId.New();

    private readonly Mock<IUserAdminService> _userAdmin = new();
    private readonly Mock<ISecurityAuditLogRepository> _auditLog = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IClock> _clock = new();

    public ApproveRegistrationCommandHandlerTests() => _clock.Setup(x => x.UtcNow).Returns(Now);

    private ApproveRegistrationCommandHandler CreateHandler() =>
        new(_userAdmin.Object, _auditLog.Object, _requestContext.Object, _clock.Object);

    [Fact]
    public async Task Handle_approves_the_registration_and_logs_who_approved_it()
    {
        var userId = UserId.New();
        _userAdmin.Setup(x => x.ApproveRegistrationAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await CreateHandler().Handle(new ApproveRegistrationCommand(userId, AdminUserId), CancellationToken.None);

        _auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == AdminUserId && e.EventType == SecurityEventType.RegistrationApproved && e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_user_does_not_exist()
    {
        _userAdmin.Setup(x => x.ApproveRegistrationAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => CreateHandler().Handle(new ApproveRegistrationCommand(UserId.New(), AdminUserId), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
