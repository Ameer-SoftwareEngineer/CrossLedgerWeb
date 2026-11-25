using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class MarkCardShippedCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");
    private static readonly UserId AdminUserId = UserId.New();

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ISecurityAuditLogRepository> _auditLog = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IClock> _clock = new();

    public MarkCardShippedCommandHandlerTests() => _clock.Setup(x => x.UtcNow).Returns(Now);

    private static Card NewApprovedPhysicalCard()
    {
        var card = new Card(
            CardId.New(), UserId.New(), WalletId.New(), Usd, CardType.Physical, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);
        card.Approve();
        return card;
    }

    private MarkCardShippedCommandHandler CreateHandler() => new(_cards.Object, _auditLog.Object, _requestContext.Object, _clock.Object);

    [Fact]
    public async Task Handle_marks_the_card_shipped_with_carrier_and_tracking_number()
    {
        var card = NewApprovedPhysicalCard();
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        await CreateHandler().Handle(new MarkCardShippedCommand(card.Id, "UPS", "1Z999", AdminUserId), CancellationToken.None);

        card.FulfillmentStatus.Should().Be(CardFulfillmentStatus.Shipped);
        card.ShippingCarrier.Should().Be("UPS");
        card.TrackingNumber.Should().Be("1Z999");
        _auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == AdminUserId && e.EventType == SecurityEventType.CardMarkedShipped && e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_card_does_not_exist()
    {
        _cards.Setup(x => x.GetByIdAsync(It.IsAny<CardId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Card?)null);

        var act = () => CreateHandler().Handle(new MarkCardShippedCommand(CardId.New(), "UPS", "1Z999", AdminUserId), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }
}
