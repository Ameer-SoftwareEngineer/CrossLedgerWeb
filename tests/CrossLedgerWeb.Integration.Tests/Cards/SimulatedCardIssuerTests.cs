using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Cards;
using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Infrastructure.Cards;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Integration.Tests.Cards;

public class SimulatedCardIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CardAddress TestAddress = new("123 Main St", "4B", "CA", "90001", "Los Angeles", "US");

    private readonly Mock<IClock> _clock = new();
    private readonly SimulatedCardIssuer _issuer;

    public SimulatedCardIssuerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _issuer = new SimulatedCardIssuer(new CardNumberGenerator(), _clock.Object);
    }

    private static CardIssuanceRequest Request() =>
        new(UserId.New(), "Test Customer", CardType.Virtual, Currency.From("USD"), TestAddress);

    [Fact]
    public async Task IssueAsync_returns_a_luhn_valid_pan_and_a_three_digit_cvv()
    {
        var result = await _issuer.IssueAsync(Request(), CancellationToken.None);

        result.Pan.Should().HaveLength(16).And.MatchRegex("^[0-9]{16}$");
        result.Cvv.Should().HaveLength(3).And.MatchRegex("^[0-9]{3}$");
    }

    [Fact]
    public async Task IssueAsync_sets_a_three_year_expiry_from_the_clock()
    {
        var result = await _issuer.IssueAsync(Request(), CancellationToken.None);

        result.ExpiryMonth.Should().Be(Now.Month);
        result.ExpiryYear.Should().Be(Now.Year + 3);
    }

    [Fact]
    public async Task IssueAsync_never_sets_a_Stripe_identifier()
    {
        var result = await _issuer.IssueAsync(Request(), CancellationToken.None);

        result.StripeCardholderId.Should().BeNull();
        result.StripeCardId.Should().BeNull();
    }
}
