using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Ledger;
using CrossLedgerWeb.Domain.Ledger;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Ledger;

public class GetTransferLedgerEntriesQueryHandlerTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly Mock<ILedgerEntryReader> _reader = new();
    private readonly Mock<IWalletRepository> _wallets = new();

    [Fact]
    public async Task Handle_returns_all_entries_when_the_caller_owns_one_of_the_wallets_touched()
    {
        var transferId = TransferId.New();
        var callerId = UserId.New();
        var sourceWalletId = WalletId.New();
        var settlementWalletId = WalletId.New();
        var entries = new[]
        {
            new LedgerEntryDetail(
                LedgerEntryId.New(), transferId, sourceWalletId, LedgerDirection.Debit, new Money(100m, Usd), new Money(-100m, Usd), Now),
            new LedgerEntryDetail(
                LedgerEntryId.New(), transferId, settlementWalletId, LedgerDirection.Credit, new Money(100m, Usd), new Money(100m, Usd), Now),
        };
        _reader.Setup(x => x.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>())).ReturnsAsync(entries);
        _wallets.Setup(x => x.GetOwnerIdAsync(sourceWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(callerId);
        _wallets.Setup(x => x.GetOwnerIdAsync(settlementWalletId, It.IsAny<CancellationToken>())).ReturnsAsync(UserId.New());
        var handler = new GetTransferLedgerEntriesQueryHandler(_reader.Object, _wallets.Object);

        var result = await handler.Handle(new GetTransferLedgerEntriesQuery(transferId, callerId), CancellationToken.None);

        result.Should().BeEquivalentTo(entries);
    }

    [Fact]
    public async Task Handle_throws_transfer_not_found_when_the_transfer_has_no_entries()
    {
        var transferId = TransferId.New();
        _reader.Setup(x => x.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<LedgerEntryDetail>());
        var handler = new GetTransferLedgerEntriesQueryHandler(_reader.Object, _wallets.Object);

        var act = () => handler.Handle(new GetTransferLedgerEntriesQuery(transferId, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<TransferNotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_transfer_not_found_when_the_caller_was_not_a_party_to_it()
    {
        var transferId = TransferId.New();
        var walletId = WalletId.New();
        var entries = new[]
        {
            new LedgerEntryDetail(LedgerEntryId.New(), transferId, walletId, LedgerDirection.Credit, new Money(50m, Usd), new Money(50m, Usd), Now),
        };
        _reader.Setup(x => x.GetByTransferIdAsync(transferId, It.IsAny<CancellationToken>())).ReturnsAsync(entries);
        _wallets.Setup(x => x.GetOwnerIdAsync(walletId, It.IsAny<CancellationToken>())).ReturnsAsync(UserId.New());
        var handler = new GetTransferLedgerEntriesQueryHandler(_reader.Object, _wallets.Object);

        var act = () => handler.Handle(new GetTransferLedgerEntriesQuery(transferId, UserId.New()), CancellationToken.None);

        await act.Should().ThrowAsync<TransferNotFoundException>();
    }
}
