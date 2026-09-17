using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ListUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_whatever_the_user_admin_service_reports()
    {
        var userAdmin = new Mock<IUserAdminService>();
        var users = new[]
        {
            new UserSummary(UserId.New(), "customer@example.com", new[] { "Customer" }, false),
            new UserSummary(UserId.New(), "admin@example.com", new[] { "Admin" }, false),
        };
        userAdmin.Setup(x => x.ListUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);
        var handler = new ListUsersQueryHandler(userAdmin.Object);

        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        result.Should().BeSameAs(users);
    }
}
