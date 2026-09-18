using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class VerifyTwoFactorLoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId User = UserId.New();

    private readonly Mock<ITwoFactorChallengeTokenValidator> _challengeValidator = new();
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<ITwoFactorCredentialRepository> _credentials = new();
    private readonly Mock<IUsedTotpCodeRepository> _usedCodes = new();
    private readonly Mock<ITotpProvider> _totp = new();
    private readonly Mock<ILoginTokenIssuer> _tokenIssuer = new();
    private readonly Mock<IClock> _clock = new();

    public VerifyTwoFactorLoginCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _challengeValidator.Setup(x => x.Validate("valid-challenge")).Returns(TwoFactorChallengeValidationResult.Valid(User));
        _identity.Setup(x => x.GetProfileAsync(User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(User, "user@example.com", ["Customer"], RegistrationStatus.Approved));
        _tokenIssuer.Setup(x => x.IssueTokens(User, "user@example.com", It.Is<IReadOnlyList<string>>(r => r.Contains("Customer"))))
            .Returns(new LoginResult("jwt", Now.AddMinutes(15), "refresh", Now.AddDays(30)));
    }

    private VerifyTwoFactorLoginCommandHandler CreateHandler() =>
        new(_challengeValidator.Object, _identity.Object, _credentials.Object, _usedCodes.Object, _totp.Object, _tokenIssuer.Object, _clock.Object);

    [Fact]
    public async Task Handle_issues_tokens_when_the_totp_code_verifies()
    {
        var credential = new TwoFactorCredential(TwoFactorCredentialId.New(), User, "SECRET", Now);
        _credentials.Setup(x => x.GetByUserIdAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _usedCodes.Setup(x => x.IsActiveAsync(User, "123456", Now, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _totp.Setup(x => x.VerifyCode("SECRET", "123456")).Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(new VerifyTwoFactorLoginCommand("valid-challenge", TwoFactorMethods.Totp, "123456"), CancellationToken.None);

        result.AccessToken.Should().Be("jwt");
        _usedCodes.Verify(x => x.Add(It.Is<UsedTotpCode>(c => c.UserId == User && c.Code == "123456")), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_totp_code_is_wrong()
    {
        var credential = new TwoFactorCredential(TwoFactorCredentialId.New(), User, "SECRET", Now);
        _credentials.Setup(x => x.GetByUserIdAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _usedCodes.Setup(x => x.IsActiveAsync(User, "000000", Now, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _totp.Setup(x => x.VerifyCode("SECRET", "000000")).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.Handle(new VerifyTwoFactorLoginCommand("valid-challenge", TwoFactorMethods.Totp, "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_totp_code_was_already_used()
    {
        var credential = new TwoFactorCredential(TwoFactorCredentialId.New(), User, "SECRET", Now);
        _credentials.Setup(x => x.GetByUserIdAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _usedCodes.Setup(x => x.IsActiveAsync(User, "123456", Now, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = CreateHandler();

        var act = () => handler.Handle(new VerifyTwoFactorLoginCommand("valid-challenge", TwoFactorMethods.Totp, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
        _totp.Verify(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_throws_when_no_totp_credential_is_enrolled()
    {
        _credentials.Setup(x => x.GetByUserIdAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync((TwoFactorCredential?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new VerifyTwoFactorLoginCommand("valid-challenge", TwoFactorMethods.Totp, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<TwoFactorNotEnabledException>();
    }

    [Fact]
    public async Task Handle_issues_tokens_when_the_sms_code_verifies()
    {
        _identity.Setup(x => x.VerifyTwoFactorSmsCodeAsync(User, "654321", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = CreateHandler();

        var result = await handler.Handle(new VerifyTwoFactorLoginCommand("valid-challenge", TwoFactorMethods.Sms, "654321"), CancellationToken.None);

        result.AccessToken.Should().Be("jwt");
    }

    [Fact]
    public async Task Handle_throws_when_the_sms_code_is_wrong()
    {
        _identity.Setup(x => x.VerifyTwoFactorSmsCodeAsync(User, "000000", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = CreateHandler();

        var act = () => handler.Handle(new VerifyTwoFactorLoginCommand("valid-challenge", TwoFactorMethods.Sms, "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_challenge_token_is_invalid()
    {
        _challengeValidator.Setup(x => x.Validate("bad-token")).Returns(TwoFactorChallengeValidationResult.Invalid);
        var handler = CreateHandler();

        var act = () => handler.Handle(new VerifyTwoFactorLoginCommand("bad-token", TwoFactorMethods.Totp, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorChallengeException>();
    }
}
