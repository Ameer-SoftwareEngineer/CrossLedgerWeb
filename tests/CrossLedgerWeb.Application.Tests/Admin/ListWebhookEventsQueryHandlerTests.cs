using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Admin;
using FluentAssertions;
using Moq;

namespace CrossLedgerWeb.Application.Tests.Admin;

public class ListWebhookEventsQueryHandlerTests
{
    [Fact]
    public async Task Handle_defaults_page_number_and_size_when_not_positive()
    {
        var events = new Mock<IProcessedWebhookEventStore>();
        var page = new WebhookEventPage(Array.Empty<WebhookEventSummary>(), 0, 1, 20);
        events.Setup(x => x.ListRecentAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(page);
        var handler = new ListWebhookEventsQueryHandler(events.Object);

        var result = await handler.Handle(new ListWebhookEventsQuery(0, -5), CancellationToken.None);

        result.Should().BeSameAs(page);
        events.Verify(x => x.ListRecentAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
