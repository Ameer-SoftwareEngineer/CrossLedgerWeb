namespace CrossLedgerWeb.Domain.ValueObjects;

public readonly record struct CardPurchaseId(Guid Value)
{
    public static CardPurchaseId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
