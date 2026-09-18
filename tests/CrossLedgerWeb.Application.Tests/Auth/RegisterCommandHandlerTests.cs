using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();

    [Fact]
    public async Task Handle_returns_the_new_user_id_on_success()
    {
        var userId = UserId.New();
        _identity.Setup(x => x.RegisterAsync(It.IsAny<RegistrationDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegistrationOutcome.Success(userId));
        var handler = new RegisterCommandHandler(_identity.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.UserId.Should().Be(userId);
        result.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Handle_throws_when_registration_fails()
    {
        _identity.Setup(x => x.RegisterAsync(It.IsAny<RegistrationDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegistrationOutcome.Failure(["Email already taken"]));
        var handler = new RegisterCommandHandler(_identity.Object);

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<RegistrationFailedException>();
    }

    private static RegisterCommand ValidCommand() => new(
        "user@example.com",
        "password123",
        "Jane Doe",
        "+15551234567",
        new DateOnly(1990, 1, 1),
        "123 Main St",
        "123 Main St",
        "Springfield",
        "IL",
        "USA",
        "UtilityBill",
        "bill.pdf",
        "application/pdf",
        [0x25, 0x50, 0x44, 0x46]);
}
