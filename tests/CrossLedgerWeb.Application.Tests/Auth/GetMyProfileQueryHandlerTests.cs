using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class GetMyProfileQueryHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();

    [Fact]
    public async Task Handle_returns_the_profile_from_the_identity_service()
    {
        var userId = UserId.New();
        var profile = new MyProfile(
            userId, "user@example.com", "Jane", null, "Doe", "+15551234567", new DateOnly(1990, 1, 1),
            "123 Main St", "123 Main St", "Springfield", "IL", "USA", RegistrationStatus.Approved);
        _identity.Setup(x => x.GetMyProfileAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        var handler = new GetMyProfileQueryHandler(_identity.Object);

        var result = await handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.Should().Be(profile);
    }
}
