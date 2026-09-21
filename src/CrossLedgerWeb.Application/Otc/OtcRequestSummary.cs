using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Otc;

public sealed record OtcRequestSummary(
    OtcRequestId OtcRequestId,
    QuoteId QuoteId,
    WalletId SourceWalletId,
    Currency FromCurrency,
    Currency ToCurrency,
    decimal RequestedAmount,
    decimal DeskRate,
    DateTimeOffset RequestedAt,
    DateTimeOffset OfferExpiresAt,
    OtcRequestStatus Status,
    Guid? TransferId,
    bool IsExpired);
