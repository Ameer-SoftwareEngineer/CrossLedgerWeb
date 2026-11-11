using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListCardsAwaitingFulfillmentQueryHandler : IRequestHandler<ListCardsAwaitingFulfillmentQuery, IReadOnlyList<CardAwaitingFulfillment>>
{
    private readonly ICardRepository _cards;
    private readonly IUserAdminService _userAdmin;

    public ListCardsAwaitingFulfillmentQueryHandler(ICardRepository cards, IUserAdminService userAdmin)
    {
        _cards = cards;
        _userAdmin = userAdmin;
    }

    public async Task<IReadOnlyList<CardAwaitingFulfillment>> Handle(ListCardsAwaitingFulfillmentQuery request, CancellationToken cancellationToken)
    {
        var cards = await _cards.ListAwaitingFulfillmentAsync(cancellationToken);
        if (cards.Count == 0)
            return Array.Empty<CardAwaitingFulfillment>();

        var users = await _userAdmin.ListUsersAsync(cancellationToken);
        var emailsByOwnerId = users.ToDictionary(u => u.Id, u => u.Email);

        return cards
            .Select(c => new CardAwaitingFulfillment(
                c.Id,
                c.OwnerId,
                emailsByOwnerId.GetValueOrDefault(c.OwnerId, "(unknown)"),
                c.CardholderName,
                c.FulfillmentStatus,
                c.ShippingCarrier,
                c.TrackingNumber,
                c.ShippingAddress,
                c.IssuedAt))
            .ToList();
    }
}
