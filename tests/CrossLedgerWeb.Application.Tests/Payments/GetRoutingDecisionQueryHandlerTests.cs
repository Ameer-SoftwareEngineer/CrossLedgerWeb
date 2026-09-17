using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Payments;

public class GetRoutingDecisionQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");

    private readonly Mock<IPayoutRepository> _payouts = new();
    private readonly Mock<IWalletRepository> _wallets = new();
    private readonly Mock<IRoutingAuditLog> _auditLog = new();

    [Fact]
    public async Task Handle_returns_the_ranked_entries_when_the_caller_owns_the_source_wallet()
    {
        var payoutId = PayoutId.New();
        var transferId = TransferId.New();
        var sourceWalletId = WalletId.New();
        var callerId = UserId.New();
        var payout = new Payout(payoutId, transferId, sourceWalletId, new Money(100m, Usd));
        _payouts.Setup(x => x.GetByIdAsync(payoutId, It.IsAny<CancellationToken>())).ReturnsAsync(payout);
        _wallets.Setup(x => x.GetOwnerIdAsync(sourceWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(callerId);
        var entries = new[]
        {
            new RoutingDecisionEntry("Airwallex", 0, 92.5m, new Money(1.5m, Usd), 30, DateTimeOffset.UtcNow),
            new RoutingDecisionEntry("Rapyd", 1, 81.0m, new Money(2.0m, Usd), 60, DateTimeOffset.UtcNow),
        };
        _auditLog.Setup(x => x.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>())).ReturnsAsync(entries);
        var handler = new GetRoutingDecisionQueryHandler(_payouts.Object, _wallets.Object, _auditLog.Object);

        var result = await handler.Handle(new GetRoutingDecisionQuery(payoutId, callerId), CancellationToken.None);

        result.Should().BeSameAs(entries);
    }

    [Fact]
    public async Task Handle_throws_payout_not_found_when_the_payout_does_not_exist()
    {
        var payoutId = PayoutId.New();
        _payouts.Setup(x => x.GetByIdAsync(payoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Payout?)null);
        var handler = new GetRoutingDecisionQueryHandler(_payouts.Object, _wallets.Object, _auditLog.Object);

        var act = () => handler.Handle(new GetRoutingDecisionQuery(payoutId, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<PayoutNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_payout_not_found_when_the_caller_does_not_own_the_source_wallet()
    {
        var payoutId = PayoutId.New();
        var sourceWalletId = WalletId.New();
        var payout = new Payout(payoutId, TransferId.New(), sourceWalletId, new Money(100m, Usd));
        _payouts.Setup(x => x.GetByIdAsync(payoutId, It.IsAny<CancellationToken>())).ReturnsAsync(payout);
        _wallets.Setup(x => x.GetOwnerIdAsync(sourceWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(UserId.New());
        var handler = new GetRoutingDecisionQueryHandler(_payouts.Object, _wallets.Object, _auditLog.Object);

        var act = () => handler.Handle(new GetRoutingDecisionQuery(payoutId, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<PayoutNotFoundException>();
        _auditLog.Verify(x => x.GetByTransferIdAsync(It.IsAny<TransferId>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
