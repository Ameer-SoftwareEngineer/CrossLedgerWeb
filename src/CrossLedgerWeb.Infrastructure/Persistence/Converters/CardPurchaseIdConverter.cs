using CrossLedgerWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossLedgerWeb.Infrastructure.Persistence.Converters;

public sealed class CardPurchaseIdConverter : ValueConverter<CardPurchaseId, Guid>
{
    public CardPurchaseIdConverter() : base(id => id.Value, value => new CardPurchaseId(value))
    {
    }
}
