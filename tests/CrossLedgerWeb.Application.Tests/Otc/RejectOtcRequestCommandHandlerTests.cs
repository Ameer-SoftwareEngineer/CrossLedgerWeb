using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Otc;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Otc;

public class RejectOtcRequestCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Pkr = Currency.From("PKR");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();

    private readonly Mock<IOtcRequestRepository> _otcRequests = new();
    private readonly OtcRequest _otcRequest;

    public RejectOtcRequestCommandHandlerTests()
    {
        _otcRequest = new OtcRequest(
            OtcRequestId.New(), Owner, WalletId.New(), QuoteId.New(), Usd, Pkr, 10_000m, 278.5m, Now, Now.AddMinutes(5));

        _otcRequests.Setup(x => x.GetByIdAsync(_otcRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_otcRequest);
    }

    private RejectOtcRequestCommandHandler CreateHandler() => new(_otcRequests.Object);

    [Fact]
    public async Task Handle_rejects_the_request()
    {
        var handler = CreateHandler();

        await handler.Handle(new RejectOtcRequestCommand(_otcRequest.Id, Owner), CancellationToken.None);

        _otcRequest.Status.Should().Be(OtcRequestStatus.Rejected);
    }

    [Fact]
    public async Task Handle_throws_when_the_request_does_not_exist()
    {
        _otcRequests.Setup(x => x.GetByIdAsync(It.IsAny<OtcRequestId>(), It.IsAny<CancellationToken>())).ReturnsAsync((OtcRequest?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RejectOtcRequestCommand(_otcRequest.Id, Owner), CancellationToken.None);

        await act.Should().ThrowAsync<OtcRequestNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_did_not_make_the_request()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(new RejectOtcRequestCommand(_otcRequest.Id, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<OtcRequestNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_request_was_already_accepted()
    {
        _otcRequest.Accept(Guid.NewGuid());
        var handler = CreateHandler();

        var act = () => handler.Handle(new RejectOtcRequestCommand(_otcRequest.Id, Owner), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOtcRequestStateException>();
    }
}
