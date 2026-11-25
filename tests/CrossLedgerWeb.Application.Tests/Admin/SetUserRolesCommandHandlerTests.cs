using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class SetUserRolesCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static (SetUserRolesCommandHandler Handler, Mock<IUserAdminService> UserAdmin, Mock<ISecurityAuditLogRepository> AuditLog) CreateHandler()
    {
        var userAdmin = new Mock<IUserAdminService>();
        var auditLog = new Mock<ISecurityAuditLogRepository>();
        var requestContext = new Mock<IRequestContextAccessor>();
        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(Now);
        return (new SetUserRolesCommandHandler(userAdmin.Object, auditLog.Object, requestContext.Object, clock.Object), userAdmin, auditLog);
    }

    [Fact]
    public async Task Handle_delegates_to_the_user_admin_service_and_logs_who_changed_the_roles()
    {
        var (handler, userAdmin, auditLog) = CreateHandler();
        var userId = UserId.New();
        var adminUserId = UserId.New();
        var roles = new[] { "Customer", "Support" };
        userAdmin.Setup(x => x.SetRolesAsync(userId, roles, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await handler.Handle(new SetUserRolesCommand(userId, roles, adminUserId), CancellationToken.None);

        userAdmin.Verify(x => x.SetRolesAsync(userId, roles, It.IsAny<CancellationToken>()), Times.Once);
        auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == adminUserId && e.EventType == SecurityEventType.RoleChanged && e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_user_not_found_when_the_service_reports_no_match()
    {
        var (handler, userAdmin, _) = CreateHandler();
        userAdmin.Setup(x => x.SetRolesAsync(It.IsAny<UserId>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => handler.Handle(new SetUserRolesCommand(UserId.New(), new[] { "Admin" }, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
