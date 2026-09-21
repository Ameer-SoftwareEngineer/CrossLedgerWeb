namespace CrossLedgerWeb.Shared.Otc;

/// <summary>Shared shape for a just-created offer and a row in the caller's OTC
/// history - Status is one of "Offered", "Accepted", "Rejected", or "Expired" (the last
/// is computed: an Offered request past OfferExpiresAt, never a persisted state).</summary>
public sealed record OtcRequestResponse(
    Guid OtcRequestId,
    Guid QuoteId,
    Guid SourceWalletId,
    string FromCurrency,
    string ToCurrency,
    decimal RequestedAmount,
    decimal DeskRate,
    DateTimeOffset RequestedAt,
    DateTimeOffset OfferExpiresAt,
    string Status,
    Guid? TransferId);
