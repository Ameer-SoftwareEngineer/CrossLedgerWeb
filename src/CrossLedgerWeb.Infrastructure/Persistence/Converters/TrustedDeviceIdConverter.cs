using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossLedgerWeb.Infrastructure.Persistence.Converters;

public sealed class TrustedDeviceIdConverter : ValueConverter<TrustedDeviceId, Guid>
{
    public TrustedDeviceIdConverter() : base(id => id.Value, value => new TrustedDeviceId(value))
    {
    }
}
