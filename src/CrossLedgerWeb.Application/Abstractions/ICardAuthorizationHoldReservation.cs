using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>Backed by dbo.usp_ReserveCardAuthorizationHold (source in
/// db/procedures/usp_ReserveCardAuthorizationHold.sql) - the only place that decides
/// whether a card authorization is approved on fund availability, and atomically inserts
/// the resulting CardAuthorizations audit row under the same locking usp_PostTransfer
/// uses, closing the TOCTOU race a plain EF read-then-insert would have. Every other
/// decline reason (card state, spending limits, MCC, country) is decided in plain C#
/// before this is ever called, since none of them race against concurrent holds.</summary>
public interface ICardAuthorizationHoldReservation
{
    Task<CardAuthorizationHoldReservationResult> ReserveAsync(
        CardAuthorizationId cardAuthorizationId,
        CardId cardId,
        WalletId walletId,
        string stripeAuthorizationId,
        string merchantName,
        Money amount,
        DateTimeOffset decidedAt,
        DateTimeOffset expiresAtIfApproved,
        CancellationToken cancellationToken);
}

public sealed record CardAuthorizationHoldReservationResult(bool Approved, string? DeclineReason);
