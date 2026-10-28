using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record RevokeTrustedDeviceCommand(UserId UserId, TrustedDeviceId TrustedDeviceId) : IRequest;
