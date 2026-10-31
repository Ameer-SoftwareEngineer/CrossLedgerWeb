using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Payments;

public class PaymentRoutingEngineTests
{
    private static readonly Currency Usd = Currency.From("USD");
    private static readonly Corridor UsToPk = new(Usd, Currency.From("PKR"), "PK");

    private readonly Mock<IProviderStatsProvider> _stats = new();
    private readonly Mock<IProviderQuoteScorer> _scorer = new();
    private readonly Mock<IRoutingAuditLog> _auditLog = new();

    public PaymentRoutingEngineTests()
    {
        _stats.Setup(x => x.GetStatsAsync(It.IsAny<ProviderCode>(), It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProviderStats.Unknown);
    }

    private static Mock<IPaymentProvider> HealthyProvider(ProviderCode code, bool supportsCorridor, ProviderQuote quote)
    {
        var provider = new Mock<IPaymentProvider>();
        provider.SetupGet(x => x.Code).Returns(code);
        provider.Setup(x => x.Supports(It.IsAny<Corridor>())).Returns(supportsCorridor);
        provider.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderHealth(code, ProviderHealthStatus.Healthy, DateTimeOffset.UtcNow));
        provider.Setup(x => x.QuoteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(quote);
        return provider;
    }

    private static PayoutRequest Request() => new(
        TransferId.New(), UsToPk, new Money(100m, Usd), "idem-key", RoutingPreference.Standard);

    [Fact]
    public async Task Selects_the_highest_scoring_provider_as_primary()
    {
        var cheapQuote = new ProviderQuote(ProviderCode.Airwallex, new Money(100m, Usd), new Money(1m, Usd), TimeSpan.FromMinutes(30));
        var expensiveQuote = new ProviderQuote(ProviderCode.Rapyd, new Money(100m, Usd), new Money(20m, Usd), TimeSpan.FromMinutes(30));
        var cheap = HealthyProvider(ProviderCode.Airwallex, supportsCorridor: true, cheapQuote);
        var expensive = HealthyProvider(ProviderCode.Rapyd, supportsCorridor: true, expensiveQuote);
        var engine = new PaymentRoutingEngine(
            [cheap.Object, expensive.Object], _stats.Object, new ProviderQuoteScorer(), _auditLog.Object);

        var decision = await engine.SelectAsync(Request(), CancellationToken.None);

        decision.Primary.Quote.ProviderCode.Should().Be(ProviderCode.Airwallex);
        decision.Fallbacks.Should().ContainSingle(x => x.Quote.ProviderCode == ProviderCode.Rapyd);
    }

    [Fact]
    public async Task Excludes_a_provider_that_does_not_support_the_corridor()
    {
        var quote = new ProviderQuote(ProviderCode.Airwallex, new Money(100m, Usd), new Money(1m, Usd), TimeSpan.FromMinutes(30));
        var unsupported = HealthyProvider(ProviderCode.Rapyd, supportsCorridor: false, quote);
        var supported = HealthyProvider(ProviderCode.Airwallex, supportsCorridor: true, quote);
        var engine = new PaymentRoutingEngine(
            [supported.Object, unsupported.Object], _stats.Object, new ProviderQuoteScorer(), _auditLog.Object);

        var decision = await engine.SelectAsync(Request(), CancellationToken.None);

        decision.Primary.Quote.ProviderCode.Should().Be(ProviderCode.Airwallex);
        unsupported.Verify(x => x.QuoteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Excludes_a_provider_whose_health_check_reports_unavailable()
    {
        var quote = new ProviderQuote(ProviderCode.Airwallex, new Money(100m, Usd), new Money(1m, Usd), TimeSpan.FromMinutes(30));
        var unhealthy = HealthyProvider(ProviderCode.Rapyd, supportsCorridor: true, quote);
        unhealthy.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderHealth(ProviderCode.Rapyd, ProviderHealthStatus.Unavailable, DateTimeOffset.UtcNow));
        var healthy = HealthyProvider(ProviderCode.Airwallex, supportsCorridor: true, quote);
        var engine = new PaymentRoutingEngine(
            [healthy.Object, unhealthy.Object], _stats.Object, new ProviderQuoteScorer(), _auditLog.Object);

        var decision = await engine.SelectAsync(Request(), CancellationToken.None);

        decision.Primary.Quote.ProviderCode.Should().Be(ProviderCode.Airwallex);
        unhealthy.Verify(x => x.QuoteAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Throws_when_no_provider_supports_the_corridor()
    {
        var quote = new ProviderQuote(ProviderCode.Airwallex, new Money(100m, Usd), new Money(1m, Usd), TimeSpan.FromMinutes(30));
        var unsupported = HealthyProvider(ProviderCode.Airwallex, supportsCorridor: false, quote);
        var engine = new PaymentRoutingEngine([unsupported.Object], _stats.Object, new ProviderQuoteScorer(), _auditLog.Object);

        var act = () => engine.SelectAsync(Request(), CancellationToken.None);

        await act.Should().ThrowAsync<NoRouteAvailableException>();
    }

    [Fact]
    public async Task Records_the_full_ranked_comparison_not_just_the_winner()
    {
        var cheapQuote = new ProviderQuote(ProviderCode.Airwallex, new Money(100m, Usd), new Money(1m, Usd), TimeSpan.FromMinutes(30));
        var expensiveQuote = new ProviderQuote(ProviderCode.Rapyd, new Money(100m, Usd), new Money(20m, Usd), TimeSpan.FromMinutes(30));
        var cheap = HealthyProvider(ProviderCode.Airwallex, supportsCorridor: true, cheapQuote);
        var expensive = HealthyProvider(ProviderCode.Rapyd, supportsCorridor: true, expensiveQuote);
        var request = Request();
        var engine = new PaymentRoutingEngine(
            [cheap.Object, expensive.Object], _stats.Object, new ProviderQuoteScorer(), _auditLog.Object);

        await engine.SelectAsync(request, CancellationToken.None);

        _auditLog.Verify(x => x.RecordAsync(
            request.TransferId,
            It.Is<IReadOnlyList<ScoredProviderQuote>>(list => list.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
