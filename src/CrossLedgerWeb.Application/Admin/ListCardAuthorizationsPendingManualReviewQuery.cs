using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ListCardAuthorizationsPendingManualReviewQuery : IRequest<IReadOnlyList<CardAuthorizationPendingManualReview>>;
