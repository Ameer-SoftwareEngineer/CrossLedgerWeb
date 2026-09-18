using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Exceptions;
using CrossLedgerWeb.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CrossLedgerWeb.Api.ExceptionHandling;

/// <summary>
/// Maps domain and application exceptions to the HTTP status codes the specification
/// calls for - e.g. QuoteExpiredException -> 409 QUOTE_EXPIRED (2.2), InsufficientFunds
/// -> a clean 409 rather than a raw 500 (2.4) - so a handler never needs its own
/// try/catch just to shape a response.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = new Dictionary<string, string[]>(validationException.Errors);
            var validationProblem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken);
            return true;
        }

        var (statusCode, title, code) = exception switch
        {
            WalletNotFoundException => (StatusCodes.Status404NotFound, "Wallet not found", "WALLET_NOT_FOUND"),
            TransferNotFoundException => (StatusCodes.Status404NotFound, "Transfer not found", "TRANSFER_NOT_FOUND"),
            UserNotFoundException => (StatusCodes.Status404NotFound, "User not found", "USER_NOT_FOUND"),
            QuoteNotFoundException => (StatusCodes.Status404NotFound, "Quote not found", "QUOTE_NOT_FOUND"),
            QuoteExpiredException => (StatusCodes.Status409Conflict, "Quote expired", "QUOTE_EXPIRED"),
            InsufficientFundsException => (StatusCodes.Status409Conflict, "Insufficient funds", "INSUFFICIENT_FUNDS"),
            CurrencyMismatchException => (StatusCodes.Status400BadRequest, "Currency mismatch", "CURRENCY_MISMATCH"),
            ExchangeRateUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Exchange rate unavailable", "RATE_UNAVAILABLE"),
            SettlementWalletNotConfiguredException => (StatusCodes.Status500InternalServerError, "Settlement wallet not configured", "SETTLEMENT_WALLET_MISSING"),
            RegistrationFailedException => (StatusCodes.Status400BadRequest, "Registration failed", "REGISTRATION_FAILED"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Invalid credentials", "INVALID_CREDENTIALS"),
            AccountLockedException => (StatusCodes.Status423Locked, "Account locked", "ACCOUNT_LOCKED"),
            AccountPendingApprovalException => (StatusCodes.Status403Forbidden, "Account pending approval", "ACCOUNT_PENDING_APPROVAL"),
            AccountRegistrationRejectedException => (StatusCodes.Status403Forbidden, "Registration rejected", "ACCOUNT_REGISTRATION_REJECTED"),
            InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, "Invalid refresh token", "INVALID_REFRESH_TOKEN"),
            RefreshTokenReuseDetectedException => (StatusCodes.Status401Unauthorized, "Refresh token reuse detected", "REFRESH_TOKEN_REUSE_DETECTED"),
            TwoFactorNotEnabledException => (StatusCodes.Status409Conflict, "Two-factor authentication not enabled", "TWO_FACTOR_NOT_ENABLED"),
            InvalidTotpCodeException => (StatusCodes.Status401Unauthorized, "Invalid authenticator code", "INVALID_TOTP_CODE"),
            TotpCodeReplayedException => (StatusCodes.Status401Unauthorized, "Authenticator code already used", "TOTP_CODE_REPLAYED"),
            RecoveryCodeAlreadyUsedException => (StatusCodes.Status409Conflict, "Recovery code already used", "RECOVERY_CODE_ALREADY_USED"),
            PayoutNotFoundException => (StatusCodes.Status404NotFound, "Payout not found", "PAYOUT_NOT_FOUND"),
            NoRouteAvailableException => (StatusCodes.Status503ServiceUnavailable, "No provider can serve this corridor", "NO_ROUTE_AVAILABLE"),
            PayoutReserveWalletNotConfiguredException => (StatusCodes.Status500InternalServerError, "Payout reserve wallet not configured", "PAYOUT_RESERVE_WALLET_MISSING"),
            InvalidPayoutTransitionException => (StatusCodes.Status409Conflict, "Invalid payout state transition", "INVALID_PAYOUT_TRANSITION"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrent update conflict", "CONCURRENCY_CONFLICT"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", "INVALID_ARGUMENT"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "UNEXPECTED_ERROR"),
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Extensions = { ["code"] = code },
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
