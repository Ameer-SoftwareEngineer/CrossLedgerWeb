using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossLedgerWeb.Infrastructure.Persistence.Converters;

/// <summary>UserIdConverter's counterpart for columns where the subject isn't always
/// resolved to a user - a failed login against an email that matches no account, for
/// example, has no UserId to store.</summary>
public sealed class NullableUserIdConverter : ValueConverter<UserId?, Guid?>
{
    public NullableUserIdConverter() : base(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new UserId(value.Value) : null)
    {
    }
}
