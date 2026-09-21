using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Otc;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Domain.Fx;
using CrossLedgerWeb.Domain.Otc;
using CrossLedgerWeb.Domain.ValueObjects;
using CrossLedgerWeb.Domain.Wallets;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Otc;

public class AcceptOtcRequestCommandHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Currency Pkr = Currency.From("PKR");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.New();

    private readonly Mock<IOtcRequestRepository> _otcRequests = new();
    private readonly Mock<IQuoteRepository> _quotes = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IFxSettlementWalletResolver> _fxSettlementWallets = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Wallet _sourceWallet;
    private readonly Wallet _targetWallet;
    private readonly Wallet _fxSettlementUsd;
    private readonly Wallet _fxSettlementPkr;
    private readonly Quote _quote;
    private readonly OtcRequest _otcRequest;

    public AcceptOtcRequestCommandHandlerTests()
    {
        _sourceWallet = new Wallet(WalletId.New(), Owner, Usd);
        _sourceWallet.Credit(new Money(20_000m, Usd), TransferId.New(), Now);
        _targetWallet = new Wallet(WalletId.New(), Owner, Pkr);
        _fxSettlementUsd = new Wallet(WalletId.New(), UserId.New(), Usd, WalletKind.SystemClearing);
        _fxSettlementPkr = new Wallet(WalletId.New(), UserId.New(), Pkr, WalletKind.SystemClearing);
        _quote = new Quote(QuoteId.New(), Usd, Pkr, midMarketRate: 279.90m, spreadRate: 0.0015m, Now, TimeSpan.FromMinutes(5));
        _otcRequest = new OtcRequest(
            OtcRequestId.New(), Owner, _sourceWallet.Id, _quote.Id, Usd, Pkr, 10_000m, _quote.CustomerRate, Now, _quote.ExpiresAt);

        _otcRequests.Setup(x => x.GetByIdAsync(_otcRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_otcRequest);
        _quotes.Setup(x => x.GetByIdAsync(_quote.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_quote);
        _wallets.Setup(x => x.GetByIdAsync(_sourceWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_sourceWallet);
        _wallets.Setup(x => x.GetByIdAsync(_targetWallet.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_targetWallet);
        _fxSettlementWallets.Setup(x => x.GetSettlementWalletAsync(Usd, It.IsAny<CancellationToken>())).ReturnsAsync(_fxSettlementUsd);
        _fxSettlementWallets.Setup(x => x.GetSettlementWalletAsync(Pkr, It.IsAny<CancellationToken>())).ReturnsAsync(_fxSettlementPkr);
        _clock.Setup(x => x.UtcNow).Returns(Now);
    }

    private AcceptOtcRequestCommandHandler CreateHandler() => new(
        _otcRequests.Object, _quotes.Object, _wallets.Object, _fxSettlementWallets.Object, _clock.Object);

    private AcceptOtcRequestCommand ValidCommand() => new(_otcRequest.Id, Owner, _targetWallet.Id, "idem-key-1");

    [Fact]
    public async Task Handle_posts_the_transfer_at_the_desk_rate_and_marks_the_request_accepted()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.SourceAmount.Should().Be(new Money(10_000m, Usd));
        result.TargetAmount.Should().Be(_quote.Convert(new Money(10_000m, Usd), Now));
        _targetWallet.Balance.Should().Be(result.TargetAmount);
        _otcRequest.Status.Should().Be(OtcRequestStatus.Accepted);
        _otcRequest.TransferId.Should().Be(result.TransferId.Value);
    }

    [Fact]
    public async Task Handle_throws_when_the_otc_request_does_not_exist()
    {
        _otcRequests.Setup(x => x.GetByIdAsync(It.IsAny<OtcRequestId>(), It.IsAny<CancellationToken>())).ReturnsAsync((OtcRequest?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<OtcRequestNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_the_same_not_found_error_when_the_caller_did_not_make_the_request()
    {
        var command = _otcRequest.Id is var id ? new AcceptOtcRequestCommand(id, UserId.New(), _targetWallet.Id, "idem-key-1") : null!;
        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<OtcRequestNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_when_the_request_was_already_accepted()
    {
        _otcRequest.Accept(Guid.NewGuid());
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOtcRequestStateException>();
    }

    [Fact]
    public async Task Handle_propagates_quote_expired_once_the_offer_window_has_passed()
    {
        _clock.Setup(x => x.UtcNow).Returns(_quote.ExpiresAt.AddSeconds(1));
        var handler = CreateHandler();

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<QuoteExpiredException>();
    }
}
