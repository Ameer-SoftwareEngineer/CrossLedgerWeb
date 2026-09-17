using CrossLedgerWeb.Application.Admin;
using CrossLedgerWeb.Application.Payments;
using CrossLedgerWeb.Domain.Payments;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class GetProviderHealthQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_the_health_reported_by_every_provider()
    {
        var simulated = new Mock<IPaymentProvider>();
        simulated.SetupGet(x => x.Code).Returns(ProviderCode.Simulated);
        simulated.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderHealth(ProviderCode.Simulated, ProviderHealthStatus.Healthy, DateTimeOffset.UtcNow));

        var airwallex = new Mock<IPaymentProvider>();
        airwallex.SetupGet(x => x.Code).Returns(ProviderCode.Airwallex);
        airwallex.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderHealth(ProviderCode.Airwallex, ProviderHealthStatus.Unavailable, DateTimeOffset.UtcNow));

        var handler = new GetProviderHealthQueryHandler(new[] { simulated.Object, airwallex.Object });

        var result = await handler.Handle(new GetProviderHealthQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(h => h.ProviderCode == ProviderCode.Simulated && h.Status == ProviderHealthStatus.Healthy);
        result.Should().Contain(h => h.ProviderCode == ProviderCode.Airwallex && h.Status == ProviderHealthStatus.Unavailable);
    }

    [Fact]
    public async Task Handle_reports_unavailable_when_a_providers_health_check_itself_throws()
    {
        var broken = new Mock<IPaymentProvider>();
        broken.SetupGet(x => x.Code).Returns(ProviderCode.Rapyd);
        broken.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("network error"));

        var handler = new GetProviderHealthQueryHandler(new[] { broken.Object });

        var result = await handler.Handle(new GetProviderHealthQuery(), CancellationToken.None);

        result.Should().ContainSingle(h => h.ProviderCode == ProviderCode.Rapyd && h.Status == ProviderHealthStatus.Unavailable);
    }
}
