using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Fx;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Fx;

public class GetFxRateOhlcQueryHandlerTests
{
    private readonly Mock<IFxRateOhlcReader> _reader = new();

    [Fact]
    public async Task Handle_passes_parsed_currencies_through_to_the_reader_and_returns_its_result()
    {
        var fromDate = new DateOnly(2026, 8, 1);
        var toDate = new DateOnly(2026, 8, 31);
        var points = new[] { new FxRateOhlcPoint(new DateOnly(2026, 8, 1), 1.10m, 1.12m, 1.08m, 1.11m) };
        _reader.Setup(x => x.GetDailyAsync(Currency.From("USD"), Currency.From("EUR"), fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);
        var handler = new GetFxRateOhlcQueryHandler(_reader.Object);

        var result = await handler.Handle(new GetFxRateOhlcQuery("USD", "EUR", fromDate, toDate), CancellationToken.None);

        result.Should().BeSameAs(points);
    }
}
