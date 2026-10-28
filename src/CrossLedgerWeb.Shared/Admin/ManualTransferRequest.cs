namespace CrossLedgerWeb.Shared.Admin;

public sealed record ManualTransferRequest(Guid SourceWalletId, Guid TargetWalletId, decimal SourceAmount, decimal TargetAmount);

public sealed record ManualTransferResponse(Guid TransferId, DateTimeOffset PostedAt);
