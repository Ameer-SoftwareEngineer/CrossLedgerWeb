using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossLedgerWeb.Infrastructure.Persistence.Converters;

public sealed class CardIdConverter : ValueConverter<CardId, Guid>
{
    public CardIdConverter() : base(id => id.Value, value => new CardId(value))
    {
    }
}
