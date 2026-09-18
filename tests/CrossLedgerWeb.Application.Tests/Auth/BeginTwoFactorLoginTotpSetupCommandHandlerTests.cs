using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class BeginTwoFactorLoginTotpSetupCommandHandlerTests
{
    private static readonly UserId User = UserId.New();

    private readonly Mock<ITwoFactorChallengeTokenValidator> _challengeValidator = new();
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IMediator> _mediator = new();

    private BeginTwoFactorLoginTotpSetupCommandHandler CreateHandler() =>
        new(_challengeValidator.Object, _identity.Object, _mediator.Object);

    [Fact]
    public async Task Handle_forwards_to_begin_totp_enrollment_for_the_challenge_tokens_user()
    {
        _challengeValidator.Setup(x => x.Validate("valid-challenge")).Returns(TwoFactorChallengeValidationResult.Valid(User));
        _identity.Setup(x => x.GetProfileAsync(User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(User, "user@example.com", ["Customer"], RegistrationStatus.Approved));
        _mediator.Setup(x => x.Send(It.Is<BeginTotpEnrollmentCommand>(c => c.UserId == User && c.Email == "user@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BeginTotpEnrollmentResult("SECRET", "otpauth://totp/..."));
        var handler = CreateHandler();

        var result = await handler.Handle(new BeginTwoFactorLoginTotpSetupCommand("valid-challenge"), CancellationToken.None);

        result.Secret.Should().Be("SECRET");
        result.QrCodeUri.Should().Be("otpauth://totp/...");
    }

    [Fact]
    public async Task Handle_throws_when_the_challenge_token_is_invalid()
    {
        _challengeValidator.Setup(x => x.Validate("bad-token")).Returns(TwoFactorChallengeValidationResult.Invalid);
        var handler = CreateHandler();

        var act = () => handler.Handle(new BeginTwoFactorLoginTotpSetupCommand("bad-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorChallengeException>();
    }
}
