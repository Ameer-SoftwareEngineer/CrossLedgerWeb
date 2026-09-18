using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class RequestPasswordResetCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();

    [Fact]
    public async Task Handle_delegates_to_identity_service()
    {
        var handler = new RequestPasswordResetCommandHandler(_identity.Object);

        await handler.Handle(new RequestPasswordResetCommand("user@example.com"), CancellationToken.None);

        _identity.Verify(x => x.RequestPasswordResetAsync("user@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }
}
