using CrossLedgerWeb.Application.Abstractions;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ListPendingCardRequestsQueryHandler : IRequestHandler<ListPendingCardRequestsQuery, IReadOnlyList<PendingCardRequest>>
{
    private readonly ICardRepository _cards;
    private readonly IUserAdminService _userAdmin;

    public ListPendingCardRequestsQueryHandler(ICardRepository cards, IUserAdminService userAdmin)
    {
        _cards = cards;
        _userAdmin = userAdmin;
    }

    public async Task<IReadOnlyList<PendingCardRequest>> Handle(ListPendingCardRequestsQuery request, CancellationToken cancellationToken)
    {
        var pendingCards = await _cards.ListPendingApprovalAsync(cancellationToken);
        if (pendingCards.Count == 0)
            return Array.Empty<PendingCardRequest>();

        // Card carries only OwnerId, not an email - the same join UserAdminService itself
        // would need, done here instead of adding an Identity dependency to ICardRepository.
        var users = await _userAdmin.ListUsersAsync(cancellationToken);
        var emailsByOwnerId = users.ToDictionary(u => u.Id, u => u.Email);

        return pendingCards
            .Select(c => new PendingCardRequest(
                c.Id,
                c.OwnerId,
                emailsByOwnerId.GetValueOrDefault(c.OwnerId, "(unknown)"),
                c.Currency.Code,
                c.CardType,
                c.CardholderName,
                c.SourceOfFunds,
                c.EstimatedGrossAnnualIncome,
                c.EstimatedNetWorth,
                c.IsActivelyEmployed,
                c.EmployerCompanyName,
                c.JobTitle,
                c.RegisteredAddress,
                c.ShippingAddress,
                c.IssuedAt))
            .ToList();
    }
}
