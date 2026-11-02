using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;

namespace CrossLedgerWeb.Infrastructure.Cards;

/// <summary>The default ICardIssuer - wraps ICardNumberGenerator's locally-generated,
/// Luhn-valid PAN exactly as IssueCardCommandHandler did before this abstraction existed.
/// Needs no external credentials, same as SimulatedProvider for payouts, and stays the
/// active issuer unless CardIssuing:UseStripe is explicitly turned on.</summary>
public sealed class SimulatedCardIssuer : ICardIssuer
{
    private const int ValidityYears = 3;

    private readonly ICardNumberGenerator _numberGenerator;
    private readonly IClock _clock;

    public SimulatedCardIssuer(ICardNumberGenerator numberGenerator, IClock clock)
    {
        _numberGenerator = numberGenerator;
        _clock = clock;
    }

    public Task<IssuedCardDetails> IssueAsync(CardIssuanceRequest request, CancellationToken cancellationToken)
    {
        var (pan, cvv) = _numberGenerator.Generate();
        var now = _clock.UtcNow;

        return Task.FromResult(new IssuedCardDetails(pan, cvv, now.Month, now.Year + ValidityYears, StripeCardholderId: null, StripeCardId: null));
    }
}
