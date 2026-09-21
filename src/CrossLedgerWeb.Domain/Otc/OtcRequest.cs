using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Domain.Otc;

/// <summary>
/// A large-trade request routed to the OTC desk rather than the retail quote flow
/// (specification 9's Ibanera-style dashboard). The desk's "negotiated" offer is really
/// just a <see cref="Fx.Quote"/> priced with a better spread than
/// CreateQuoteCommandHandler's retail 0.50% and held open longer - accepting it posts a
/// transfer through that same Quote, exactly like the regular Send Money flow, so this
/// entity is a thin request/offer/accept wrapper around one, not a parallel pricing or
/// settlement path.
/// </summary>
public sealed class OtcRequest
{
    public OtcRequestId Id { get; }
    public UserId RequestedByUserId { get; }
    public WalletId SourceWalletId { get; }
    public QuoteId QuoteId { get; }
    public Currency FromCurrency { get; }
    public Currency ToCurrency { get; }
    public decimal RequestedAmount { get; }

    /// <summary>Copied from the underlying Quote's CustomerRate at creation, so listing a
    /// user's OTC history never has to join back to Quotes - the rate that was actually
    /// offered, same reasoning as Quote.CustomerRate itself being persisted rather than
    /// recomputed.</summary>
    public decimal DeskRate { get; }

    public DateTimeOffset RequestedAt { get; }
    public DateTimeOffset OfferExpiresAt { get; }
    public OtcRequestStatus Status { get; private set; }
    public Guid? TransferId { get; private set; }

    public OtcRequest(
        OtcRequestId id,
        UserId requestedByUserId,
        WalletId sourceWalletId,
        QuoteId quoteId,
        Currency fromCurrency,
        Currency toCurrency,
        decimal requestedAmount,
        decimal deskRate,
        DateTimeOffset requestedAt,
        DateTimeOffset offerExpiresAt)
    {
        if (fromCurrency == toCurrency)
            throw new ArgumentException("An OTC request requires two different currencies.", nameof(toCurrency));

        if (requestedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedAmount), requestedAmount, "The requested amount must be positive.");

        if (offerExpiresAt <= requestedAt)
            throw new ArgumentOutOfRangeException(nameof(offerExpiresAt), offerExpiresAt, "The offer must expire after it was requested.");

        Id = id;
        RequestedByUserId = requestedByUserId;
        SourceWalletId = sourceWalletId;
        QuoteId = quoteId;
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        RequestedAmount = requestedAmount;
        DeskRate = deskRate;
        RequestedAt = requestedAt;
        OfferExpiresAt = offerExpiresAt;
        Status = OtcRequestStatus.Offered;
    }

    /// <summary>Rehydrates a request exactly as it was persisted, bypassing the public
    /// constructor's validation. Used by EF Core materialization only.</summary>
    private OtcRequest(
        OtcRequestId id,
        UserId requestedByUserId,
        WalletId sourceWalletId,
        QuoteId quoteId,
        Currency fromCurrency,
        Currency toCurrency,
        decimal requestedAmount,
        decimal deskRate,
        DateTimeOffset requestedAt,
        DateTimeOffset offerExpiresAt,
        OtcRequestStatus status,
        Guid? transferId)
    {
        Id = id;
        RequestedByUserId = requestedByUserId;
        SourceWalletId = sourceWalletId;
        QuoteId = quoteId;
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        RequestedAmount = requestedAmount;
        DeskRate = deskRate;
        RequestedAt = requestedAt;
        OfferExpiresAt = offerExpiresAt;
        Status = status;
        TransferId = transferId;
    }

    public bool IsExpired(DateTimeOffset asOf) => Status == OtcRequestStatus.Offered && asOf > OfferExpiresAt;

    /// <summary>Marks the offer accepted once its transfer has actually posted - the
    /// caller runs TransferPoster.Post (which itself rejects a stale Quote) first and
    /// passes in the resulting transfer id, so this never marks a request Accepted
    /// without money having moved.</summary>
    public void Accept(Guid transferId)
    {
        if (Status != OtcRequestStatus.Offered)
            throw new InvalidOtcRequestStateException(Id, Status);

        Status = OtcRequestStatus.Accepted;
        TransferId = transferId;
    }

    public void Reject()
    {
        if (Status != OtcRequestStatus.Offered)
            throw new InvalidOtcRequestStateException(Id, Status);

        Status = OtcRequestStatus.Rejected;
    }
}
