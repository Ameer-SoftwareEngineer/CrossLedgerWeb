using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class RejectCardRequestCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");
    private static readonly UserId AdminUserId = UserId.New();

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ISecurityAuditLogRepository> _auditLog = new();
    private readonly Mock<IRequestContextAccessor> _requestContext = new();
    private readonly Mock<IClock> _clock = new();

    public RejectCardRequestCommandHandlerTests() => _clock.Setup(x => x.UtcNow).Returns(Now);

    private static Card NewPendingCard() => new(
        CardId.New(), UserId.New(), WalletId.New(), Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
        SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);

    private RejectCardRequestCommandHandler CreateHandler() => new(_cards.Object, _auditLog.Object, _requestContext.Object, _clock.Object);

    [Fact]
    public async Task Handle_rejects_the_card_and_logs_who_rejected_it()
    {
        var card = NewPendingCard();
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        await CreateHandler().Handle(new RejectCardRequestCommand(card.Id, AdminUserId), CancellationToken.None);

        card.Status.Should().Be(CardStatus.Rejected);
        _auditLog.Verify(x => x.RecordAsync(
            It.Is<SecurityAuditEvent>(e => e.UserId == AdminUserId && e.EventType == SecurityEventType.CardRequestRejected && e.Succeeded),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_card_does_not_exist()
    {
        _cards.Setup(x => x.GetByIdAsync(It.IsAny<CardId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Card?)null);

        var act = () => CreateHandler().Handle(new RejectCardRequestCommand(CardId.New(), AdminUserId), CancellationToken.None);

        await act.Should().ThrowAsync<CardNotFoundException>();
    }
}
