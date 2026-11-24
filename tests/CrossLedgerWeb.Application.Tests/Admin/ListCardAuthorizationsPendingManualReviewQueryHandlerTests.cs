using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ListCardAuthorizationsPendingManualReviewQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardAuthorizationRepository> _authorizations = new();
    private readonly Mock<ICardRepository> _cards = new();

    private ListCardAuthorizationsPendingManualReviewQueryHandler CreateHandler() => new(_authorizations.Object, _cards.Object);

    private static Card NewCard(string cardholderName) => new(
        CardId.New(), UserId.New(), WalletId.New(), Usd, CardType.Virtual, cardholderName, "4532750123456784", "123", 1, 2029, Now,
        SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);

    [Fact]
    public async Task Handle_returns_an_empty_list_when_nothing_is_pending()
    {
        _authorizations.Setup(x => x.ListPendingManualReviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CardAuthorization>());

        var result = await CreateHandler().Handle(new ListCardAuthorizationsPendingManualReviewQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        _cards.Verify(x => x.GetByIdAsync(It.IsAny<CardId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_joins_the_cardholder_name_for_each_pending_row()
    {
        var card = NewCard("Test Customer");
        var authorization = new CardAuthorization(
            CardAuthorizationId.New(), card.Id, "iauth_test", new Money(50m, Usd), "Stripe Coffee Co",
            approved: true, declineReason: null, holdDuration: TimeSpan.FromDays(7), decidedAt: Now.AddDays(-1));
        authorization.FlagForManualReview("insufficient_funds", 75m, Now.AddHours(-1));

        _authorizations.Setup(x => x.ListPendingManualReviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { authorization });
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        var result = await CreateHandler().Handle(new ListCardAuthorizationsPendingManualReviewQuery(), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Match<CardAuthorizationPendingManualReview>(r =>
            r.CardAuthorizationId == authorization.Id
            && r.CardholderName == "Test Customer"
            && r.MerchantName == "Stripe Coffee Co"
            && r.Amount == 50m
            && r.AttemptedCaptureAmount == 75m
            && r.ManualReviewReason == "insufficient_funds");
    }
}
