using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Fx;
using CrossLedgerWeb.Application.Otc;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Otc;

public class CreateOtcRequestCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Pkr = Currency.From("PKR");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();
    private static readonly WalletId SourceWalletId = WalletId.New();

    private readonly Mock<IExchangeRateProvider> _rates = new();
    private readonly Mock<IQuoteRepository> _quotes = new();
    private readonly Mock<IOtcRequestRepository> _otcRequests = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IClock> _clock = new();

    public CreateOtcRequestCommandHandlerTests()
    {
        _wallets.Setup(x => x.GetOwnerIdAsync(SourceWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(Owner);
        _rates.Setup(x => x.GetRateAsync(Usd, Pkr, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRateReading(Usd, Pkr, 279.90m, Now, IsStale: false));
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private CreateOtcRequestCommandHandler CreateHandler() =>
        new(_rates.Object, _quotes.Object, _otcRequests.Object, _wallets.Object, _clock.Object);

    private static CreateOtcRequestCommand ValidCommand() => new(Owner, SourceWalletId, "USD", "PKR", 10_000m);

    [Fact]
    public async Task Handle_offers_a_tighter_spread_than_the_retail_quote()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // mid-market 279.90, desk spread 0.15% -> better than the retail 0.50% spread's 278.5005.
        result.DeskRate.Should().BeGreaterThan(278.5005m);
        result.RequestedAmount.Should().Be(10_000m);
    }

    [Fact]
    public async Task Handle_holds_the_offer_open_longer_than_a_retail_quote()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.OfferExpiresAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public async Task Handle_stages_both_the_quote_and_the_otc_request_for_insertion()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        _quotes.Verify(x => x.Add(It.Is<Quote>(q => q.Id == result.QuoteId)), Times.Once);
        _otcRequests.Verify(x => x.Add(It.Is<OtcRequest>(r => r.Id == result.OtcRequestId)), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_the_source_wallet_does_not_exist()
    {
        _wallets.Setup(x => x.GetOwnerIdAsync(SourceWalletId, It.IsAny<CancellationToken>())).ReturnsAsync((UserId?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_wallet_belongs_to_someone_else()
    {
        _wallets.Setup(x => x.GetOwnerIdAsync(SourceWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(UserId.New());
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<WalletNotFoundException>();
    }
}
