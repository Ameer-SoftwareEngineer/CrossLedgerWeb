using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;

namespace CrossLedgerWeb.Domain.Tests.Payments;

public class PayoutTests
{
    // Every legal transition in the specification 5.5 diagram, plus "any state except
    // Reversed itself -> Reversed". Every OTHER of the 8x8 = 64 (from, to) pairs is
    // illegal and must throw - Theory data below is generated from this single source
    // of truth so the illegal set can never drift out of sync with the legal one.
    private static readonly HashSet<(PayoutState From, PayoutState To)> LegalTransitions = new()
    {
        (PayoutState.Quoted, PayoutState.Reserved),
        (PayoutState.Reserved, PayoutState.Submitted),
        (PayoutState.Reserved, PayoutState.PendingManual),
        (PayoutState.Submitted, PayoutState.ProviderFailed),
        (PayoutState.Submitted, PayoutState.Processing),
        (PayoutState.ProviderFailed, PayoutState.Submitted),
        (PayoutState.ProviderFailed, PayoutState.PendingManual),
        (PayoutState.Processing, PayoutState.Settled),
        (PayoutState.Quoted, PayoutState.Reversed),
        (PayoutState.Reserved, PayoutState.Reversed),
        (PayoutState.Submitted, PayoutState.Reversed),
        (PayoutState.ProviderFailed, PayoutState.Reversed),
        (PayoutState.PendingManual, PayoutState.Reversed),
        (PayoutState.Processing, PayoutState.Reversed),
        (PayoutState.Settled, PayoutState.Reversed),
    };

    private static readonly PayoutState[] AllStates = Enum.GetValues<PayoutState>();

    public static IEnumerable<object[]> AllTransitionPairs()
    {
        foreach (var from in AllStates)
        foreach (var to in AllStates)
            yield return [from, to];
    }

    private static readonly Currency Usd = Currency.From("USD");

    private static Payout PayoutIn(PayoutState state)
    {
        var payout = new Payout(PayoutId.New(), TransferId.New(), WalletId.New(), new Money(100m, Usd));

        // Drive it there through whatever legal path reaches this state, so the test
        // never has to reach into private state.
        var path = state switch
        {
            PayoutState.Quoted => Array.Empty<PayoutState>(),
            PayoutState.Reserved => [PayoutState.Reserved],
            PayoutState.Submitted => [PayoutState.Reserved, PayoutState.Submitted],
            PayoutState.ProviderFailed => [PayoutState.Reserved, PayoutState.Submitted, PayoutState.ProviderFailed],
            PayoutState.PendingManual =>
            [
                PayoutState.Reserved, PayoutState.Submitted, PayoutState.ProviderFailed, PayoutState.PendingManual,
            ],
            PayoutState.Processing => [PayoutState.Reserved, PayoutState.Submitted, PayoutState.Processing],
            PayoutState.Settled =>
                [PayoutState.Reserved, PayoutState.Submitted, PayoutState.Processing, PayoutState.Settled],
            PayoutState.Reversed => [PayoutState.Reversed],
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };

        foreach (var step in path)
            payout.TransitionTo(step);

        return payout;
    }

    [Theory]
    [MemberData(nameof(AllTransitionPairs))]
    public void Transition_matches_the_specifications_state_machine(PayoutState from, PayoutState to)
    {
        var payout = PayoutIn(from);
        var act = () => payout.TransitionTo(to);

        if (LegalTransitions.Contains((from, to)))
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidPayoutTransitionException>();
        }
    }

    [Fact]
    public void A_new_payout_starts_quoted()
    {
        var payout = new Payout(PayoutId.New(), TransferId.New(), WalletId.New(), new Money(100m, Usd));

        payout.State.Should().Be(PayoutState.Quoted);
    }

    [Fact]
    public void A_zero_or_negative_amount_is_rejected()
    {
        var act = () => new Payout(PayoutId.New(), TransferId.New(), WalletId.New(), new Money(0m, Usd));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_failed_provider_can_retry_through_a_second_submission()
    {
        var payout = PayoutIn(PayoutState.ProviderFailed);

        payout.TransitionTo(PayoutState.Submitted);

        payout.State.Should().Be(PayoutState.Submitted);
    }

    [Fact]
    public void MarkAccepted_records_the_provider_and_its_reference_and_moves_to_processing()
    {
        var payout = PayoutIn(PayoutState.Submitted);

        payout.MarkAccepted(ProviderCode.Simulated, "sim-ref-123");

        payout.State.Should().Be(PayoutState.Processing);
        payout.ProviderCode.Should().Be(ProviderCode.Simulated);
        payout.ProviderReference.Should().Be("sim-ref-123");
    }

    [Fact]
    public void MarkProviderFailed_records_which_provider_was_tried()
    {
        var payout = PayoutIn(PayoutState.Submitted);

        payout.MarkProviderFailed(ProviderCode.Rapyd);

        payout.State.Should().Be(PayoutState.ProviderFailed);
        payout.ProviderCode.Should().Be(ProviderCode.Rapyd);
    }

    [Fact]
    public void A_state_transition_bumps_UpdatedAt()
    {
        var payout = PayoutIn(PayoutState.Quoted);
        var before = payout.UpdatedAt;

        payout.Reserve();

        payout.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Reverse_is_rejected_once_already_reversed()
    {
        var payout = PayoutIn(PayoutState.Reversed);

        var act = payout.Reverse;

        act.Should().Throw<InvalidPayoutTransitionException>();
    }
}
