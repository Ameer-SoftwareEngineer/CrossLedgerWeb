using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossLedgerWeb.Infrastructure.Persistence.Converters;

public sealed class CardAuthorizationIdConverter : ValueConverter<CardAuthorizationId, Guid>
{
    public CardAuthorizationIdConverter() : base(id => id.Value, value => new CardAuthorizationId(value))
    {
    }
}
