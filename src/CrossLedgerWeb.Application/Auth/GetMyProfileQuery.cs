using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record GetMyProfileQuery(UserId UserId) : IRequest<MyProfile?>;
