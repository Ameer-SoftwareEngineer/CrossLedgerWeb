using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Cards;

public sealed record CardIssuanceRequest(
    UserId OwnerId, string CardholderName, CardType CardType, Currency Currency, CardAddress BillingAddress);
