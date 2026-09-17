using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record SetUserRolesCommand(UserId UserId, IReadOnlyList<string> Roles) : IRequest;
