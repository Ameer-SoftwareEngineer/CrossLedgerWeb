using CrossLedgerWeb.Application.Cards;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Issues the actual PAN/CVV/expiry a new Card is constructed with - the
/// Simulated-vs-real split this codebase already uses for payment providers
/// (IPaymentProvider/SimulatedProvider), applied to card issuance. Selected via
/// configuration (CardIssuing:UseStripe); SimulatedCardIssuer is always available and
/// needs no external credentials, same as SimulatedProvider for payouts.</summary>
public interface ICardIssuer
{
    Task<IssuedCardDetails> IssueAsync(CardIssuanceRequest request, CancellationToken cancellationToken);
}
