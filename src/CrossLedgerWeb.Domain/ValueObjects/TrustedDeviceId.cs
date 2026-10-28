namespace CrossLedgerWeb.Domain.ValueObjects;

public readonly record struct TrustedDeviceId(Guid Value)
{
    public static TrustedDeviceId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
