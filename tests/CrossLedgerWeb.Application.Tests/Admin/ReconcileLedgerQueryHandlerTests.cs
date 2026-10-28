using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ReconcileLedgerQueryHandlerTests
{
    private readonly Mock<ILedgerReconciliationReader> _reader = new();

    [Fact]
    public async Task Handle_returns_an_empty_list_when_the_ledger_is_balanced()
    {
        _reader.Setup(x => x.ReconcileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LedgerImbalance>());
        var handler = new ReconcileLedgerQueryHandler(_reader.Object);

        var result = await handler.Handle(new ReconcileLedgerQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_surfaces_whatever_imbalances_the_reader_reports()
    {
        var imbalances = new[] { new LedgerImbalance("USD", 0.01m) };
        _reader.Setup(x => x.ReconcileAsync(It.IsAny<CancellationToken>())).ReturnsAsync(imbalances);
        var handler = new ReconcileLedgerQueryHandler(_reader.Object);

        var result = await handler.Handle(new ReconcileLedgerQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(imbalances);
    }
}
