using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Admin;

public sealed record CardAwaitingFulfillment(
    CardId CardId,
    UserId OwnerId,
    string OwnerEmail,
    string CardholderName,
    CardFulfillmentStatus FulfillmentStatus,
    string? ShippingCarrier,
    string? TrackingNumber,
    CardAddress ShippingAddress,
    DateTimeOffset IssuedAt);
