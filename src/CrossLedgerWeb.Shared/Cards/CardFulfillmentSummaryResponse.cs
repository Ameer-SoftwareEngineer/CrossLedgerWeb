namespace CrossLedgerWeb.Shared.Cards;

public sealed record CardFulfillmentSummaryResponse(
    Guid CardId,
    Guid OwnerId,
    string OwnerEmail,
    string CardholderName,
    string FulfillmentStatus,
    string? ShippingCarrier,
    string? TrackingNumber,
    CardAddressDto ShippingAddress,
    DateTimeOffset IssuedAt);
