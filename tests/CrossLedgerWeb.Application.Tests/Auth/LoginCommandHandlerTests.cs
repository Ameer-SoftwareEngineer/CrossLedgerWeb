using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IClock> _clock = new();

    public LoginCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private LoginCommandHandler CreateHandler() =>
        new(_identity.Object, _jwt.Object, _refreshTokens.Object, _clock.Object);

    [Fact]
    public async Task Handle_issues_an_access_token_and_a_refresh_token_on_valid_credentials()
    {
        var userId = UserId.New();
        _identity.Setup(x => x.ValidateCredentialsAsync("user@example.com", "correct-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationOutcome.Success(userId));
        _identity.Setup(x => x.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(userId, "user@example.com", ["Customer"], RegistrationStatus.Approved));
        _jwt.Setup(x => x.GenerateAccessToken(userId, "user@example.com", It.Is<IReadOnlyList<string>>(r => r.Contains("Customer"))))
            .Returns(new AccessToken("jwt-value", Now.AddMinutes(15)));
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        result.AccessToken.Should().Be("jwt-value");
        result.AccessTokenExpiresAt.Should().Be(Now.AddMinutes(15));
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshTokenExpiresAt.Should().Be(Now + LoginCommandHandler.RefreshTokenValidity);
        _refreshTokens.Verify(x => x.Add(It.Is<RefreshToken>(t => t.UserId == userId)), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_invalid_credentials_when_validation_fails()
    {
        _identity.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationOutcome.Failed);
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand("user@example.com", "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_throws_account_locked_when_the_account_is_locked_out()
    {
        _identity.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationOutcome.LockedOut);
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand("user@example.com", "password"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountLockedException>();
    }

    [Fact]
    public async Task Handle_throws_account_pending_approval_when_registration_is_not_yet_reviewed()
    {
        var userId = UserId.New();
        _identity.Setup(x => x.ValidateCredentialsAsync("user@example.com", "correct-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationOutcome.Success(userId));
        _identity.Setup(x => x.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(userId, "user@example.com", ["Customer"], RegistrationStatus.Pending));
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountPendingApprovalException>();
    }

    [Fact]
    public async Task Handle_throws_account_registration_rejected_when_an_admin_rejected_the_kyc_review()
    {
        var userId = UserId.New();
        _identity.Setup(x => x.ValidateCredentialsAsync("user@example.com", "correct-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationOutcome.Success(userId));
        _identity.Setup(x => x.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(userId, "user@example.com", ["Customer"], RegistrationStatus.Rejected));
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountRegistrationRejectedException>();
    }
}
