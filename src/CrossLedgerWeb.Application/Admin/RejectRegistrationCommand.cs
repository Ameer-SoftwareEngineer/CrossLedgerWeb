using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record RejectRegistrationCommand(UserId UserId, UserId AdminUserId) : IRequest;
