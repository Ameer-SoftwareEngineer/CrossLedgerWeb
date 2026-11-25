using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Application.Exceptions;
using CrossLedgerWeb.Domain.Security;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed class ManualTransferCommandHandler : IRequestHandler<ManualTransferCommand, ManualTransferCommandResult>
{
    private readonly IWalletRepository _wallets;
    private readonly IFxSettlementWalletResolver _fxSettlementWallets;
    private readonly IManualTransferPoster _poster;
    private readonly ISecurityAuditLogRepository _auditLog;
    private readonly IRequestContextAccessor _requestContext;
    private readonly IClock _clock;

    public ManualTransferCommandHandler(
        IWalletRepository wallets,
        IFxSettlementWalletResolver fxSettlementWallets,
        IManualTransferPoster poster,
        ISecurityAuditLogRepository auditLog,
        IRequestContextAccessor requestContext,
        IClock clock)
    {
        _wallets = wallets;
        _fxSettlementWallets = fxSettlementWallets;
        _poster = poster;
        _auditLog = auditLog;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<ManualTransferCommandResult> Handle(ManualTransferCommand request, CancellationToken cancellationToken)
    {
        var sourceOwnership = await _wallets.GetOwnershipAsync(request.SourceWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.SourceWalletId);

        var targetOwnership = await _wallets.GetOwnershipAsync(request.TargetWalletId, cancellationToken)
            ?? throw new WalletNotFoundException(request.TargetWalletId);

        var fxSettlementSource = await _fxSettlementWallets.GetSettlementWalletAsync(sourceOwnership.Currency, cancellationToken);
        var fxSettlementTarget = await _fxSettlementWallets.GetSettlementWalletAsync(targetOwnership.Currency, cancellationToken);

        var transferId = TransferId.New();
        var now = _clock.UtcNow;

        var result = await _poster.PostAsync(
            transferId,
            request.SourceWalletId,
            fxSettlementSource.Id,
            fxSettlementTarget.Id,
            request.TargetWalletId,
            request.SourceAmount,
            sourceOwnership.Currency,
            request.TargetAmount,
            targetOwnership.Currency,
            now,
            cancellationToken);

        await _auditLog.RecordAsync(
            new SecurityAuditEvent(
                SecurityAuditEventId.New(), request.AdminUserId,
                $"Posted manual transfer {result.TransferId}: {request.SourceAmount} {sourceOwnership.Currency.Code} -> {request.TargetAmount} {targetOwnership.Currency.Code}",
                SecurityEventType.ManualTransferPosted, succeeded: true, failureReason: null,
                _requestContext.IpAddress, _requestContext.UserAgent, now),
            cancellationToken);

        return new ManualTransferCommandResult(result.TransferId, result.PostedAt);
    }
}
