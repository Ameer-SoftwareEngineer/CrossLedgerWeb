using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ResolveCardAuthorizationManualReviewCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICardAuthorizationRepository> _authorizations = new();
    private readonly Mock<IClock> _clock = new();

    public ResolveCardAuthorizationManualReviewCommandHandlerTests() => _clock.Setup(x => x.UtcNow).Returns(Now);

    private static CardAuthorization NewPendingManualReview()
    {
        var authorization = new CardAuthorization(
            CardAuthorizationId.New(), CardId.New(), "iauth_test", new Money(50m, Usd), "Stripe Coffee Co",
            approved: true, declineReason: null, holdDuration: TimeSpan.FromDays(7), decidedAt: Now.AddDays(-1));
        authorization.FlagForManualReview("insufficient_funds", 75m, Now.AddHours(-1));
        return authorization;
    }

    private ResolveCardAuthorizationManualReviewCommandHandler CreateHandler() => new(_authorizations.Object, _clock.Object);

    [Fact]
    public async Task Handle_resolves_a_pending_manual_review_row()
    {
        var authorization = NewPendingManualReview();
        _authorizations.Setup(x => x.GetByIdAsync(authorization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(authorization);

        await CreateHandler().Handle(new ResolveCardAuthorizationManualReviewCommand(authorization.Id), CancellationToken.None);

        authorization.HoldStatus.Should().Be(CardAuthorizationHoldStatus.ManuallyResolved);
        authorization.ResolvedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_throws_when_the_authorization_does_not_exist()
    {
        _authorizations.Setup(x => x.GetByIdAsync(It.IsAny<CardAuthorizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardAuthorization?)null);

        var act = () => CreateHandler().Handle(new ResolveCardAuthorizationManualReviewCommand(CardAuthorizationId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<CardAuthorizationNotFoundException>();
    }
}
