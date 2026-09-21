using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossLedgerWeb.Infrastructure.Persistence.Converters;

public sealed class OtcRequestIdConverter : ValueConverter<OtcRequestId, Guid>
{
    public OtcRequestIdConverter() : base(id => id.Value, value => new OtcRequestId(value))
    {
    }
}
