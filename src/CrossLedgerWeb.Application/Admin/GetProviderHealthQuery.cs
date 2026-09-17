using CrossLedgerWeb.Application.Payments;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record GetProviderHealthQuery : IRequest<IReadOnlyList<ProviderHealth>>;
