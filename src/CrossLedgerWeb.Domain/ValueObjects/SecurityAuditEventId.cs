namespace CrossLedgerWeb.Domain.ValueObjects;

public readonly record struct SecurityAuditEventId(Guid Value)
{
    public static SecurityAuditEventId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
