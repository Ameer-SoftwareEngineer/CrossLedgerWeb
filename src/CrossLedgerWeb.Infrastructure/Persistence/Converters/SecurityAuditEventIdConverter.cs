using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossLedgerWeb.Infrastructure.Persistence.Converters;

public sealed class SecurityAuditEventIdConverter : ValueConverter<SecurityAuditEventId, Guid>
{
    public SecurityAuditEventIdConverter() : base(id => id.Value, value => new SecurityAuditEventId(value))
    {
    }
}
