using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class ConfirmTwoFactorLoginTotpSetupCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId User = UserId.New();

    private readonly Mock<ITwoFactorChallengeTokenValidator> _challengeValidator = new();
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<ILoginTokenIssuer> _tokenIssuer = new();
    private readonly Mock<IMediator> _mediator = new();

    private ConfirmTwoFactorLoginTotpSetupCommandHandler CreateHandler() =>
        new(_challengeValidator.Object, _identity.Object, _tokenIssuer.Object, _mediator.Object);

    [Fact]
    public async Task Handle_enrolls_totp_and_issues_tokens_when_the_challenge_and_code_are_valid()
    {
        _challengeValidator.Setup(x => x.Validate("valid-challenge")).Returns(TwoFactorChallengeValidationResult.Valid(User));
        _mediator.Setup(x => x.Send(It.Is<ConfirmTotpEnrollmentCommand>(c => c.UserId == User && c.Secret == "SECRET" && c.Code == "123456"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmTotpEnrollmentResult(["code1", "code2"]));
        _identity.Setup(x => x.GetProfileAsync(User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(User, "user@example.com", ["Customer"], RegistrationStatus.Approved));
        _tokenIssuer.Setup(x => x.IssueTokens(User, "user@example.com", It.Is<IReadOnlyList<string>>(r => r.Contains("Customer"))))
            .Returns(new LoginResult("jwt", Now.AddMinutes(15), "refresh", Now.AddDays(30)));
        var handler = CreateHandler();

        var result = await handler.Handle(new ConfirmTwoFactorLoginTotpSetupCommand("valid-challenge", "SECRET", "123456"), CancellationToken.None);

        result.Tokens.AccessToken.Should().Be("jwt");
        result.RecoveryCodes.Should().BeEquivalentTo(["code1", "code2"]);
    }

    [Fact]
    public async Task Handle_throws_when_the_challenge_token_is_invalid()
    {
        _challengeValidator.Setup(x => x.Validate("bad-token")).Returns(TwoFactorChallengeValidationResult.Invalid);
        var handler = CreateHandler();

        var act = () => handler.Handle(new ConfirmTwoFactorLoginTotpSetupCommand("bad-token", "SECRET", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorChallengeException>();
        _mediator.Verify(x => x.Send(It.IsAny<ConfirmTotpEnrollmentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
