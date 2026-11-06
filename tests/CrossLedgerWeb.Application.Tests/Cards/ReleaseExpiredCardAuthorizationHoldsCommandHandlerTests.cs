using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Cards;

public class ReleaseExpiredCardAuthorizationHoldsCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICardAuthorizationRepository> _authorizations = new();
    private readonly Mock<IClock> _clock = new();

    public ReleaseExpiredCardAuthorizationHoldsCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private ReleaseExpiredCardAuthorizationHoldsCommandHandler CreateHandler() => new(_authorizations.Object, _clock.Object);

    private static CardAuthorization NewOpenHold(DateTimeOffset decidedAt, TimeSpan holdDuration) =>
        new(CardAuthorizationId.New(), CardId.New(), $"iauth_{Guid.NewGuid()}", new Money(25m, Currency.From("USD")),
            "Stripe Coffee Co", approved: true, declineReason: null, holdDuration: holdDuration, decidedAt: decidedAt);

    [Fact]
    public async Task Handle_releases_only_holds_the_repository_reports_as_open_and_expired()
    {
        var expired1 = NewOpenHold(Now.AddDays(-10), TimeSpan.FromDays(7));
        var expired2 = NewOpenHold(Now.AddDays(-8), TimeSpan.FromDays(7));
        _authorizations.Setup(x => x.ListOpenAndExpiredAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { expired1, expired2 });

        var handler = CreateHandler();
        var releasedCount = await handler.Handle(new ReleaseExpiredCardAuthorizationHoldsCommand(), CancellationToken.None);

        releasedCount.Should().Be(2);
        expired1.HoldStatus.Should().Be(CardAuthorizationHoldStatus.Released);
        expired1.ResolvedAt.Should().Be(Now);
        expired2.HoldStatus.Should().Be(CardAuthorizationHoldStatus.Released);
    }

    [Fact]
    public async Task Handle_returns_zero_when_nothing_is_expired()
    {
        _authorizations.Setup(x => x.ListOpenAndExpiredAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CardAuthorization>());

        var handler = CreateHandler();
        var releasedCount = await handler.Handle(new ReleaseExpiredCardAuthorizationHoldsCommand(), CancellationToken.None);

        releasedCount.Should().Be(0);
    }
}
