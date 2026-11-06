using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Stripe's own identifiers and the captured amount, straight off the Transaction
/// object a issuing_transaction.created webhook carries (specification 12.3/12.4) -
/// CapturedAmountMinorUnits/CurrencyCode arrive as Stripe sent them (integer smallest-
/// currency-unit, lowercase ISO code), same convention DecideCardAuthorizationCommand
/// already uses for the original authorization amount.</summary>
public sealed record CaptureCardAuthorizationCommand(
    string StripeAuthorizationId,
    string StripeTransactionId,
    long CapturedAmountMinorUnits,
    string CurrencyCode) : IRequest<CaptureCardAuthorizationResult>;
