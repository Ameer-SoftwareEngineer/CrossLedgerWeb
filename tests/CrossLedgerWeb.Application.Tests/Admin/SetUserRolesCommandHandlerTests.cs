using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class SetUserRolesCommandHandlerTests
{
    [Fact]
    public async Task Handle_delegates_to_the_user_admin_service()
    {
        var userAdmin = new Mock<IUserAdminService>();
        var userId = UserId.New();
        var roles = new[] { "Customer", "Support" };
        userAdmin.Setup(x => x.SetRolesAsync(userId, roles, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new SetUserRolesCommandHandler(userAdmin.Object);

        await handler.Handle(new SetUserRolesCommand(userId, roles), CancellationToken.None);

        userAdmin.Verify(x => x.SetRolesAsync(userId, roles, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_user_not_found_when_the_service_reports_no_match()
    {
        var userAdmin = new Mock<IUserAdminService>();
        userAdmin.Setup(x => x.SetRolesAsync(It.IsAny<UserId>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new SetUserRolesCommandHandler(userAdmin.Object);

        var act = () => handler.Handle(new SetUserRolesCommand(UserId.New(), new[] { "Admin" }), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
