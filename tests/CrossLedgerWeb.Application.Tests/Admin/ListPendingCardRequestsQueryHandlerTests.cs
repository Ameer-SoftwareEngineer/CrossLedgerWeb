using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ListPendingCardRequestsQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<IUserAdminService> _userAdmin = new();

    private ListPendingCardRequestsQueryHandler CreateHandler() => new(_cards.Object, _userAdmin.Object);

    [Fact]
    public async Task Handle_resolves_the_owners_email_from_the_user_list()
    {
        var owner = UserId.New();
        var card = new Card(
            CardId.New(), owner, WalletId.New(), Usd, CardType.Virtual, "Test Customer", "4532750123456784", "123", 1, 2029, Now,
            SourceOfFunds.Salary, 5000m, 100000m, true, "Acme Inc", "Developer", TestAddress, TestAddress);

        _cards.Setup(x => x.ListPendingApprovalAsync(It.IsAny<CancellationToken>())).ReturnsAsync([card]);
        _userAdmin.Setup(x => x.ListUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserSummary(owner, "owner@example.com", ["Customer"], false)]);

        var result = await CreateHandler().Handle(new ListPendingCardRequestsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].OwnerEmail.Should().Be("owner@example.com");
        result[0].Id.Should().Be(card.Id);
    }

    [Fact]
    public async Task Handle_returns_empty_without_calling_user_admin_when_nothing_is_pending()
    {
        _cards.Setup(x => x.ListPendingApprovalAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Card>());

        var result = await CreateHandler().Handle(new ListPendingCardRequestsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        _userAdmin.Verify(x => x.ListUsersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
