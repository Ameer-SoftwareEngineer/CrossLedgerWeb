using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record RequestPasswordResetCommand(string Email) : IRequest;
