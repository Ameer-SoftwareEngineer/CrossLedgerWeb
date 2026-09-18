using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class SendLoginSmsCodeCommandHandlerTests
{
    private static readonly UserId User = UserId.New();

    private readonly Mock<ITwoFactorChallengeTokenValidator> _challengeValidator = new();
    private readonly Mock<IIdentityService> _identity = new();

    private SendLoginSmsCodeCommandHandler CreateHandler() => new(_challengeValidator.Object, _identity.Object);

    [Fact]
    public async Task Handle_sends_the_code_for_the_challenge_tokens_user()
    {
        _challengeValidator.Setup(x => x.Validate("valid-challenge")).Returns(TwoFactorChallengeValidationResult.Valid(User));
        var handler = CreateHandler();

        await handler.Handle(new SendLoginSmsCodeCommand("valid-challenge"), CancellationToken.None);

        _identity.Verify(x => x.SendTwoFactorSmsCodeAsync(User, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_challenge_token_is_invalid()
    {
        _challengeValidator.Setup(x => x.Validate("bad-token")).Returns(TwoFactorChallengeValidationResult.Invalid);
        var handler = CreateHandler();

        var act = () => handler.Handle(new SendLoginSmsCodeCommand("bad-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorChallengeException>();
        _identity.Verify(x => x.SendTwoFactorSmsCodeAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
