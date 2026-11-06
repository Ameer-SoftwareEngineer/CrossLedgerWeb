namespace CrossLedgerWeb.Domain.ValueObjects;

public readonly record struct CardAuthorizationId(Guid Value)
{
    public static CardAuthorizationId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
