using CrossLedgerWeb.Application.Abstractions;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

public sealed record AcceptOtcRequestCommand(
    OtcRequestId OtcRequestId,
    UserId CallerId,
    WalletId TargetWalletId,
    string IdempotencyKey) : IRequest<AcceptOtcRequestResult>, IIdempotentRequest;
