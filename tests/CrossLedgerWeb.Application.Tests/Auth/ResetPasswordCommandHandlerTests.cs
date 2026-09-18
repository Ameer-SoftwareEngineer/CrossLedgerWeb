using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();

    private ResetPasswordCommandHandler CreateHandler() => new(_identity.Object);

    [Fact]
    public async Task Handle_succeeds_when_the_identity_service_resets_the_password()
    {
        _identity.Setup(x => x.ResetPasswordAsync("user@example.com", "valid-token", "NewP@ssword1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler();

        var act = () => handler.Handle(new ResetPasswordCommand("user@example.com", "valid-token", "NewP@ssword1"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_throws_when_the_token_is_invalid()
    {
        _identity.Setup(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = CreateHandler();

        var act = () => handler.Handle(new ResetPasswordCommand("user@example.com", "bad-token", "NewP@ssword1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPasswordResetException>();
    }
}
