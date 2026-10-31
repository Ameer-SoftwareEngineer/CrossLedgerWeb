using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ListWebhookDeliveriesQueryHandlerTests
{
    [Fact]
    public async Task Handle_defaults_page_number_and_size_when_not_positive()
    {
        var notifier = new Mock<IWebhookNotifier>();
        var page = new WebhookDeliveryPage(Array.Empty<WebhookDeliverySummary>(), 0, 1, 20);
        notifier.Setup(x => x.ListRecentAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(page);
        var handler = new ListWebhookDeliveriesQueryHandler(notifier.Object);

        var result = await handler.Handle(new ListWebhookDeliveriesQuery(0, -5), CancellationToken.None);

        result.Should().BeSameAs(page);
        notifier.Verify(x => x.ListRecentAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
