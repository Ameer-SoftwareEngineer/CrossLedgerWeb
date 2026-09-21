using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Otc;


public sealed record CreateOtcRequestResult(
    OtcRequestId OtcRequestId,
    QuoteId QuoteId,
    WalletId SourceWalletId,
    Currency FromCurrency,
    Currency ToCurrency,
    decimal RequestedAmount,
    decimal DeskRate,
    DateTimeOffset RequestedAt,
    DateTimeOffset OfferExpiresAt);
