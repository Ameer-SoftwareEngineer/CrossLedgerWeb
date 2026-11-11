namespace CrossLedgerWeb.Shared.Cards;

/// <summary>Null keeps the old card's shipping address.</summary>
public sealed record ReissueCardRequest(CardAddressDto? NewShippingAddress);

public sealed record ReissueCardResponse(Guid NewCardId);
