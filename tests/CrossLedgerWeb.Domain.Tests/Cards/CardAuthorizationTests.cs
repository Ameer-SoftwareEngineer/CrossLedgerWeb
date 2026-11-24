using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;

namespace CrossLedgerWeb.Domain.Tests.Cards;

public class CardAuthorizationTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset DecidedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static CardAuthorization NewOpenHold(decimal amount = 100m) =>
        new(CardAuthorizationId.New(), CardId.New(), "iauth_test", new Money(amount, Usd), "Stripe Coffee Co",
            approved: true, declineReason: null, holdDuration: TimeSpan.FromDays(7), decidedAt: DecidedAt);

    [Fact]
    public void FlagForManualReview_transitions_an_open_hold_to_pending_manual_review()
    {
        var authorization = NewOpenHold(amount: 50m);
        var flaggedAt = DecidedAt.AddDays(1);

        authorization.FlagForManualReview("insufficient_funds", attemptedCaptureAmount: 75m, flaggedAt);

        authorization.HoldStatus.Should().Be(CardAuthorizationHoldStatus.PendingManualReview);
        authorization.ManualReviewReason.Should().Be("insufficient_funds");
        authorization.AttemptedCaptureAmount.Should().Be(75m);
        authorization.ManualReviewFlaggedAt.Should().Be(flaggedAt);
        authorization.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void FlagForManualReview_throws_when_the_hold_is_not_open()
    {
        var authorization = NewOpenHold();
        authorization.Release(DecidedAt.AddDays(1));

        var act = () => authorization.FlagForManualReview("insufficient_funds", 75m, DecidedAt.AddDays(2));

        act.Should().Throw<InvalidCardAuthorizationHoldStateException>();
    }

    [Fact]
    public void ResolveManualReview_transitions_pending_manual_review_to_manually_resolved()
    {
        var authorization = NewOpenHold();
        authorization.FlagForManualReview("insufficient_funds", 75m, DecidedAt.AddDays(1));
        var resolvedAt = DecidedAt.AddDays(2);

        authorization.ResolveManualReview(resolvedAt);

        authorization.HoldStatus.Should().Be(CardAuthorizationHoldStatus.ManuallyResolved);
        authorization.ResolvedAt.Should().Be(resolvedAt);
    }

    [Fact]
    public void ResolveManualReview_throws_when_the_hold_is_not_pending_manual_review()
    {
        var authorization = NewOpenHold();

        var act = () => authorization.ResolveManualReview(DecidedAt.AddDays(1));

        act.Should().Throw<InvalidCardAuthorizationHoldStateException>();
    }
}
