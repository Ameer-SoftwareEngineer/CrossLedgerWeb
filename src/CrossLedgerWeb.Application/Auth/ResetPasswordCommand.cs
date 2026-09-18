using MediatR;

namespace CrossLedgerWeb.Application.Auth;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;
