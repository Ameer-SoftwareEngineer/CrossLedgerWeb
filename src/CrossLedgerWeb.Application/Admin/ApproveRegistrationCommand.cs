using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ApproveRegistrationCommand(UserId UserId) : IRequest;
