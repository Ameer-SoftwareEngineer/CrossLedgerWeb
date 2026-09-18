using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record GetKycDocumentQuery(UserId UserId) : IRequest<KycDocument?>;
