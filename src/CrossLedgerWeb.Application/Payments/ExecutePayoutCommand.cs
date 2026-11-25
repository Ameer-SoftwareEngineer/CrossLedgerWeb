using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.Payments;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Payments;

public sealed record ExecutePayoutCommand(
    WalletId SourceWalletId,
    string TargetCurrencyCode,
    string DestinationCountry,
    decimal Amount,
    RoutingPreference Preference,
    UserId CallerId,
    string IdempotencyKey) : IRequest<ExecutePayoutResult>, IIdempotentRequest;

public sealed record ExecutePayoutResult(
    PayoutId PayoutId,
    TransferId TransferId,
    PayoutState State,
    ProviderCode? ProviderCode,
    string? ProviderReference);
