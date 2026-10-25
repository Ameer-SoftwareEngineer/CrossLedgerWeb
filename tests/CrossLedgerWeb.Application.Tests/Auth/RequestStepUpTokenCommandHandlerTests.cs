using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class RequestStepUpTokenCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId User = UserId.New();

    private readonly Mock<ITwoFactorCredentialRepository> _credentials = new();
    private readonly Mock<IUsedTotpCodeRepository> _usedCodes = new();
    private readonly Mock<ITotpProvider> _totp = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<ISecurityAuditLogRepository> _auditLog = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IClock> _clock = new();

    public RequestStepUpTokenCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private RequestStepUpTokenCommandHandler CreateHandler() =>
        new(_credentials.Object, _usedCodes.Object, _totp.Object, _jwt.Object, _auditLog.Object, _requestContext.Object, _clock.Object);

    private void SetUpEnabledCredential(string secret = "SECRET123") =>
        _credentials.Setup(x => x.GetByUserIdAsync(User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TwoFactorCredential(TwoFactorCredentialId.New(), User, secret, Now.AddDays(-1)));

    [Fact]
    public async Task Handle_issues_a_step_up_token_when_the_code_is_fresh_and_valid()
    {
        SetUpEnabledCredential();
        _usedCodes.Setup(x => x.IsActiveAsync(User, "123456", Now, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _totp.Setup(x => x.VerifyCode("SECRET123", "123456")).Returns(true);
        _jwt.Setup(x => x.GenerateStepUpToken(User, StepUpOperation.HighValueTransfer))
            .Returns(new AccessToken("step-up-jwt", Now.AddMinutes(5)));
        var handler = CreateHandler();

        var result = await handler.Handle(new RequestStepUpTokenCommand(User, StepUpOperation.HighValueTransfer, "123456"), CancellationToken.None);

        result.StepUpToken.Should().Be("step-up-jwt");
        result.ExpiresAt.Should().Be(Now.AddMinutes(5));
        _usedCodes.Verify(x => x.Add(It.Is<UsedTotpCode>(c => c.UserId == User && c.Code == "123456")), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_two_factor_is_not_enabled()
    {
        _credentials.Setup(x => x.GetByUserIdAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync((TwoFactorCredential?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RequestStepUpTokenCommand(User, StepUpOperation.HighValueTransfer, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<TwoFactorNotEnabledException>();
    }

    [Fact]
    public async Task Handle_throws_on_replay_without_calling_verify_code_again()
    {
        SetUpEnabledCredential();
        _usedCodes.Setup(x => x.IsActiveAsync(User, "123456", Now, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RequestStepUpTokenCommand(User, StepUpOperation.HighValueTransfer, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<TotpCodeReplayedException>();
        _totp.Verify(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_throws_and_does_not_record_the_code_when_it_fails_to_verify()
    {
        SetUpEnabledCredential();
        _usedCodes.Setup(x => x.IsActiveAsync(User, "000000", Now, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _totp.Setup(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RequestStepUpTokenCommand(User, StepUpOperation.HighValueTransfer, "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTotpCodeException>();
        _usedCodes.Verify(x => x.Add(It.IsAny<UsedTotpCode>()), Times.Never);
    }

    [Fact]
    public async Task Handle_records_a_successful_audit_event_when_the_code_verifies()
    {
        SetUpEnabledCredential();
        _usedCodes.Setup(x => x.IsActiveAsync(User, "123456", Now, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _totp.Setup(x => x.VerifyCode("SECRET123", "123456")).Returns(true);
        _jwt.Setup(x => x.GenerateStepUpToken(User, StepUpOperation.HighValueTransfer))
            .Returns(new AccessToken("step-up-jwt", Now.AddMinutes(5)));
        var handler = CreateHandler();

        await handler.Handle(new RequestStepUpTokenCommand(User, StepUpOperation.HighValueTransfer, "123456"), CancellationToken.None);

        _auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == User && e.EventType == SecurityEventType.StepUpToken && e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_records_a_failed_audit_event_when_the_code_fails_to_verify()
    {
        SetUpEnabledCredential();
        _usedCodes.Setup(x => x.IsActiveAsync(User, "000000", Now, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _totp.Setup(x => x.VerifyCode(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RequestStepUpTokenCommand(User, StepUpOperation.HighValueTransfer, "000000"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidTotpCodeException>();

        _auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == User && e.EventType == SecurityEventType.StepUpToken && !e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
