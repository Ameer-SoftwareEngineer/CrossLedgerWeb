using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Stripe's own identifiers and amount, straight off the issuing_authorization.request
/// webhook's pending_request (specification 12.3) - AmountMinorUnits/CurrencyCode arrive as
/// Stripe sent them (integer smallest-currency-unit, lowercase ISO code) so the minor-unit
/// conversion happens once, in the handler, next to the Card whose Currency.DecimalPlaces it
/// needs.</summary>
public sealed record DecideCardAuthorizationCommand(
    string StripeAuthorizationId,
    string StripeCardId,
    long AmountMinorUnits,
    string CurrencyCode,
    string MerchantName) : IRequest<DecideCardAuthorizationResult>;
