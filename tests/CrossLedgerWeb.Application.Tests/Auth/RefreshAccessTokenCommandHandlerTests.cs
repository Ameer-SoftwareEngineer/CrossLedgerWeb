using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Auth;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Auth;

public class RefreshAccessTokenCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId User = UserId.New();

    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    public RefreshAccessTokenCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _identity.Setup(x => x.GetProfileAsync(User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(User, "user@example.com", ["Customer"], RegistrationStatus.Approved));
        _jwt.Setup(x => x.GenerateAccessToken(User, "user@example.com", It.IsAny<IReadOnlyList<string>>()))
            .Returns(new AccessToken("new-jwt", Now.AddMinutes(15)));
    }

    private RefreshAccessTokenCommandHandler CreateHandler() =>
        new(_identity.Object, _jwt.Object, _refreshTokens.Object, _unitOfWork.Object, _clock.Object);

    private (RefreshToken Token, string Raw) IssueActiveToken(Guid? familyId = null)
    {
        var raw = RefreshTokenGenerator.GenerateRawToken();
        var token = new RefreshToken(
            RefreshTokenId.New(), User, RefreshTokenGenerator.Hash(raw), familyId ?? Guid.NewGuid(), Now.AddDays(-1), TimeSpan.FromDays(30));
        return (token, raw);
    }

    [Fact]
    public async Task Handle_rotates_an_active_token_and_returns_a_new_pair()
    {
        var (token, raw) = IssueActiveToken();
        _refreshTokens.Setup(x => x.GetByTokenHashAsync(RefreshTokenGenerator.Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _refreshTokens.Setup(x => x.GetFamilyAsync(token.FamilyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);
        var handler = CreateHandler();

        var result = await handler.Handle(new RefreshAccessTokenCommand(raw), CancellationToken.None);

        result.AccessToken.Should().Be("new-jwt");
        result.RefreshToken.Should().NotBe(raw);
        token.IsRevoked.Should().BeTrue();
        _refreshTokens.Verify(x => x.Add(It.Is<RefreshToken>(t => t.FamilyId == token.FamilyId)), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_token_is_unknown()
    {
        _refreshTokens.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshAccessTokenCommand("unknown-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_propagates_reuse_detection_and_revokes_the_family()
    {
        var (token, raw) = IssueActiveToken();
        token.Revoke(Now.AddHours(-1)); // already used once before - this call is a replay
        var sibling = IssueActiveToken(token.FamilyId).Token;
        _refreshTokens.Setup(x => x.GetByTokenHashAsync(RefreshTokenGenerator.Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _refreshTokens.Setup(x => x.GetFamilyAsync(token.FamilyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([token, sibling]);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshAccessTokenCommand(raw), CancellationToken.None);

        await act.Should().ThrowAsync<RefreshTokenReuseDetectedException>();
        sibling.IsRevoked.Should().BeTrue();
    }

    // Regression test: UnitOfWorkBehavior only commits after a *successful* next() - it
    // never runs when a handler throws. Rotate() revokes the family in memory before
    // throwing RefreshTokenReuseDetectedException, so without the handler's own explicit
    // save that revocation would be computed and then silently discarded rather than
    // reaching the database. Found by running this scenario against a real SQL Server
    // end to end: the "revoked" sibling token kept working on a second reuse attempt.
    [Fact]
    public async Task Handle_persists_the_family_revocation_before_rethrowing_on_reuse()
    {
        var (token, raw) = IssueActiveToken();
        token.Revoke(Now.AddHours(-1));
        var sibling = IssueActiveToken(token.FamilyId).Token;
        _refreshTokens.Setup(x => x.GetByTokenHashAsync(RefreshTokenGenerator.Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _refreshTokens.Setup(x => x.GetFamilyAsync(token.FamilyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([token, sibling]);
        var handler = CreateHandler();

        try
        {
            await handler.Handle(new RefreshAccessTokenCommand(raw), CancellationToken.None);
        }
        catch (RefreshTokenReuseDetectedException)
        {
            // expected
        }

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
