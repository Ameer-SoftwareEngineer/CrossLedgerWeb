using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Payments;

/// <summary>
/// The lifecycle of a single external payout attempt (specification 5.5) - distinct
/// from <see cref="Ledger.TransferId"/>'s internal wallet-to-wallet movement. Transitions
/// are enforced here, not by convention: an invalid one throws rather than silently
/// corrupting the payout's recorded state. The named methods below (Reserve, Submit,
/// MarkAccepted, ...) exist so typical call sites state intent rather than an opaque
/// target state; TransitionTo itself stays public for callers - and tests - that need to
/// drive or verify the full state machine generically.
/// </summary>
public sealed class Payout
{
    private static readonly IReadOnlyDictionary<PayoutState, PayoutState[]> AllowedTransitions =
        new Dictionary<PayoutState, PayoutState[]>
        {
            [PayoutState.Quoted] = [PayoutState.Reserved],
            // PendingManual directly from Reserved covers a routing failure severe enough
            // that no provider is ever actually submitted to (e.g. none supports the
            // corridor at all) - specification 5.5's own diagram only shows PendingManual
            // reached via a chain of provider attempts, but funds are reserved either way
            // and both cases need the same operator attention.
            [PayoutState.Reserved] = [PayoutState.Submitted, PayoutState.PendingManual],
            [PayoutState.Submitted] = [PayoutState.ProviderFailed, PayoutState.Processing],
            [PayoutState.ProviderFailed] = [PayoutState.Submitted, PayoutState.PendingManual],
            [PayoutState.PendingManual] = [],
            [PayoutState.Processing] = [PayoutState.Settled],
            [PayoutState.Settled] = [],
            [PayoutState.Reversed] = [],
        };

    public PayoutId Id { get; }
    public TransferId TransferId { get; }
    public WalletId SourceWalletId { get; }
    public Money Amount { get; }
    public PayoutState State { get; private set; }

    /// <summary>The most recent provider this payout was submitted to, successfully or
    /// not (specification 5.4's routing chain) - null until the first submission. Only the
    /// latest attempt is kept: a payout that fails over from Stripe to Rapyd ends up
    /// recording Rapyd, not Stripe's earlier failure. That is the one gap in computing
    /// specification 5.3's rolling-success-rate-per-provider directly from this table - see
    /// Infrastructure's ProviderStatsReader for how that is handled.</summary>
    public ProviderCode? ProviderCode { get; private set; }

    /// <summary>The provider's own reference for this attempt, set on acceptance - this is
    /// what an inbound webhook correlates back to a specific Payout.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>When State last changed - specification 5.3's rolling 24h provider stats
    /// are windowed against this. An operational bookkeeping timestamp, not a ledger entry
    /// (see LedgerEntry/Wallet for why those take an explicit postedAt instead): nothing
    /// downstream needs it to be deterministic, so it is stamped directly from the system
    /// clock rather than threaded through every transition call site, including the state
    /// machine's own 64-case theory test.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    public Payout(PayoutId id, TransferId transferId, WalletId sourceWalletId, Money amount)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("A payout amount must be positive.", nameof(amount));

        Id = id;
        TransferId = transferId;
        SourceWalletId = sourceWalletId;
        Amount = amount;
        State = PayoutState.Quoted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Rehydration constructor for EF Core materialization - see LedgerEntry's
    /// equivalent for why: Amount maps as a complex property EF cannot bind through a
    /// constructor parameter, so this constructor omits it and EF sets it directly via the
    /// backing field afterward. Validation is intentionally skipped - a row that made it
    /// into storage was already valid when created.</summary>
    private Payout(PayoutId id, TransferId transferId, WalletId sourceWalletId, PayoutState state, ProviderCode? providerCode, string? providerReference)
    {
        Id = id;
        TransferId = transferId;
        SourceWalletId = sourceWalletId;
        State = state;
        ProviderCode = providerCode;
        ProviderReference = providerReference;
        Amount = default;
    }

    /// <summary>"RESERVED - funds held, ledger entries written" (5.5). Posting the actual
    /// entries is PayoutReserver's job, alongside constructing the Payout itself - this
    /// method only records that the state now matches what PayoutReserver just did.</summary>
    public void Reserve() => TransitionTo(PayoutState.Reserved);

    /// <summary>About to call a provider's ExecuteAsync.</summary>
    public void Submit() => TransitionTo(PayoutState.Submitted);

    /// <summary>A provider accepted the payout (specification 5.4) - "Processing", not
    /// "Settled": acceptance only means the provider acknowledged the request, not that
    /// money has actually arrived. Settlement is confirmed later, by a webhook.</summary>
    public void MarkAccepted(ProviderCode providerCode, string providerReference)
    {
        ProviderCode = providerCode;
        ProviderReference = providerReference;
        TransitionTo(PayoutState.Processing);
    }

    /// <summary>A provider deterministically rejected the payout - the caller fails over
    /// to the next provider in the routing chain (specification 5.4) by calling Submit()
    /// again once ready, or MarkPendingManual() if the chain is exhausted. Records which
    /// provider this was, same as MarkAccepted, so a payout that ultimately lands on
    /// PendingManual still shows which provider was last tried.</summary>
    public void MarkProviderFailed(ProviderCode providerCode)
    {
        ProviderCode = providerCode;
        TransitionTo(PayoutState.ProviderFailed);
    }

    /// <summary>Every provider in the chain was exhausted (specification 5.4) - "funds stay
    /// reserved but unspent, and an operations alert is raised."</summary>
    public void MarkPendingManual() => TransitionTo(PayoutState.PendingManual);

    /// <summary>A settlement webhook confirmed the money actually arrived.</summary>
    public void MarkSettled() => TransitionTo(PayoutState.Settled);

    /// <summary>"Any state -> REVERSED posts compensating ledger entries" (5.5).</summary>
    public void Reverse() => TransitionTo(PayoutState.Reversed);

    public void TransitionTo(PayoutState newState)
    {
        // "Any state -> REVERSED" (5.5) is the one exception to the table below. Reversed
        // itself is terminal: no double-reversal.
        if (newState == PayoutState.Reversed)
        {
            if (State == PayoutState.Reversed)
                throw new InvalidPayoutTransitionException(State, newState);

            State = newState;
            UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (!AllowedTransitions.TryGetValue(State, out var allowed) || !allowed.Contains(newState))
            throw new InvalidPayoutTransitionException(State, newState);

        State = newState;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
