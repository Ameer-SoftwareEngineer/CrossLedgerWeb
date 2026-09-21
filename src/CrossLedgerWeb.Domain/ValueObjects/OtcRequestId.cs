namespace CrossLedgerWeb.Domain.ValueObjects;

public readonly record struct OtcRequestId(Guid Value)
{
    public static OtcRequestId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
