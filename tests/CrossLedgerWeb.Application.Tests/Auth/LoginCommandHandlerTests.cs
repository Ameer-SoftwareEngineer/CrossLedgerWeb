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
    private readonly Mock<ITwoFactorCredentialRepository> _twoFactorCredentials = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();

    private LoginCommandHandler CreateHandler() =>
        new(_identity.Object, _twoFactorCredentials.Object, _jwt.Object);

    private void SetUpValidCredentials(UserId userId, RegistrationStatus status = RegistrationStatus.Approved)
    {
        _identity.Setup(x => x.ValidateCredentialsAsync("user@example.com", "correct-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CredentialValidationOutcome.Success(userId));
        _identity.Setup(x => x.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(userId, "user@example.com", ["Customer"], status));
    }

    [Fact]
    public async Task Handle_requires_setup_when_no_two_factor_method_is_enrolled()
    {
        var userId = UserId.New();
        SetUpValidCredentials(userId);
        _twoFactorCredentials.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((TwoFactorCredential?)null);
        _identity.Setup(x => x.GetTwoFactorPhoneStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TwoFactorPhoneStatus(false, null));
        _jwt.Setup(x => x.GenerateTwoFactorChallengeToken(userId)).Returns(new AccessToken("challenge-value", Now.AddMinutes(10)));
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        result.RequiresSetup.Should().BeTrue();
        result.AvailableMethods.Should().BeEquivalentTo(TwoFactorMethods.All);
        result.ChallengeToken.Should().Be("challenge-value");
    }

    [Fact]
    public async Task Handle_offers_only_totp_when_only_totp_is_enrolled()
    {
        var userId = UserId.New();
        SetUpValidCredentials(userId);
        var credential = new TwoFactorCredential(TwoFactorCredentialId.New(), userId, "SECRET", Now);
        _twoFactorCredentials.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _identity.Setup(x => x.GetTwoFactorPhoneStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TwoFactorPhoneStatus(false, null));
        _jwt.Setup(x => x.GenerateTwoFactorChallengeToken(userId)).Returns(new AccessToken("challenge-value", Now.AddMinutes(10)));
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        result.RequiresSetup.Should().BeFalse();
        result.AvailableMethods.Should().BeEquivalentTo([TwoFactorMethods.Totp]);
    }

    [Fact]
    public async Task Handle_offers_sms_with_a_masked_phone_number_when_the_phone_is_verified()
    {
        var userId = UserId.New();
        SetUpValidCredentials(userId);
        _twoFactorCredentials.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((TwoFactorCredential?)null);
        _identity.Setup(x => x.GetTwoFactorPhoneStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TwoFactorPhoneStatus(true, "••••4567"));
        _jwt.Setup(x => x.GenerateTwoFactorChallengeToken(userId)).Returns(new AccessToken("challenge-value", Now.AddMinutes(10)));
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        result.RequiresSetup.Should().BeFalse();
        result.AvailableMethods.Should().BeEquivalentTo([TwoFactorMethods.Sms]);
        result.MaskedPhoneNumber.Should().Be("••••4567");
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
        SetUpValidCredentials(userId, RegistrationStatus.Pending);
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountPendingApprovalException>();
    }

    [Fact]
    public async Task Handle_throws_account_registration_rejected_when_an_admin_rejected_the_kyc_review()
    {
        var userId = UserId.New();
        SetUpValidCredentials(userId, RegistrationStatus.Rejected);
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand("user@example.com", "correct-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountRegistrationRejectedException>();
    }
}
