using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;

namespace CrossLedgerWeb.Domain.Tests.Otc;

public class OtcRequestTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Pkr = Currency.From("PKR");
    private static readonly DateTimeOffset RequestedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static OtcRequest NewRequest(TimeSpan? validFor = null) => new(
        OtcRequestId.New(), UserId.New(), WalletId.New(), QuoteId.New(),
        Usd, Pkr, 10_000m, 278.5m, RequestedAt, RequestedAt + (validFor ?? TimeSpan.FromMinutes(5)));

    [Fact]
    public void Constructor_starts_the_request_as_Offered()
    {
        var request = NewRequest();

        request.Status.Should().Be(OtcRequestStatus.Offered);
        request.TransferId.Should().BeNull();
    }

    [Fact]
    public void Constructor_rejects_the_same_currency_on_both_sides()
    {
        var act = () => new OtcRequest(
            OtcRequestId.New(), UserId.New(), WalletId.New(), QuoteId.New(),
            Usd, Usd, 10_000m, 1m, RequestedAt, RequestedAt.AddMinutes(5));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_rejects_a_non_positive_requested_amount()
    {
        var act = () => new OtcRequest(
            OtcRequestId.New(), UserId.New(), WalletId.New(), QuoteId.New(),
            Usd, Pkr, 0m, 1m, RequestedAt, RequestedAt.AddMinutes(5));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_rejects_an_offer_expiry_that_is_not_after_the_request_time()
    {
        var act = () => new OtcRequest(
            OtcRequestId.New(), UserId.New(), WalletId.New(), QuoteId.New(),
            Usd, Pkr, 10_000m, 1m, RequestedAt, RequestedAt);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsExpired_is_false_before_the_offer_expires()
    {
        var request = NewRequest(TimeSpan.FromMinutes(5));

        request.IsExpired(RequestedAt.AddMinutes(4)).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_is_true_after_the_offer_expires()
    {
        var request = NewRequest(TimeSpan.FromMinutes(5));

        request.IsExpired(request.OfferExpiresAt.AddSeconds(1)).Should().BeTrue();
    }

    [Fact]
    public void Accept_transitions_to_Accepted_and_records_the_transfer_id()
    {
        var request = NewRequest();
        var transferId = Guid.NewGuid();

        request.Accept(transferId);

        request.Status.Should().Be(OtcRequestStatus.Accepted);
        request.TransferId.Should().Be(transferId);
    }

    [Fact]
    public void Accept_throws_when_the_request_is_not_Offered()
    {
        var request = NewRequest();
        request.Accept(Guid.NewGuid());

        var act = () => request.Accept(Guid.NewGuid());

        act.Should().Throw<InvalidOtcRequestStateException>();
    }

    [Fact]
    public void Reject_transitions_to_Rejected()
    {
        var request = NewRequest();

        request.Reject();

        request.Status.Should().Be(OtcRequestStatus.Rejected);
    }

    [Fact]
    public void Reject_throws_when_the_request_is_not_Offered()
    {
        var request = NewRequest();
        request.Reject();

        var act = () => request.Reject();

        act.Should().Throw<InvalidOtcRequestStateException>();
    }
}
