using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

/// <summary>An admin-only direct transfer between two wallets, posted through
/// dbo.usp_PostTransfer rather than a Quote - for support/ops corrections where the
/// admin specifies both amounts explicitly (same figures for a same-currency move; an
/// explicit manual rate for a cross-currency one). Never reachable by a Customer.
/// Idempotency-key protected like every other money-moving endpoint (specification
/// 2.3) - a double-submitted correction must not post twice.</summary>
public sealed record ManualTransferCommand(
    WalletId SourceWalletId,
    WalletId TargetWalletId,
    decimal SourceAmount,
    decimal TargetAmount,
    string IdempotencyKey) : IRequest<ManualTransferCommandResult>, IIdempotentRequest;

public sealed record ManualTransferCommandResult(TransferId TransferId, DateTimeOffset PostedAt);
