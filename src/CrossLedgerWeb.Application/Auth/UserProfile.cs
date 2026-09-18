using CrossLedgerWeb.Domain.Auth;
using CrossLedgerWeb.Domain.ValueObjects;

namespace CrossLedgerWeb.Application.Auth;

public sealed record UserProfile(UserId UserId, string Email, IReadOnlyList<string> Roles, RegistrationStatus RegistrationStatus);
