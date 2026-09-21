using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

public sealed record CreateOtcRequestCommand(
    UserId RequestedByUserId,
    WalletId SourceWalletId,
    string FromCurrency,
    string ToCurrency,
    decimal Amount) : IRequest<CreateOtcRequestResult>;
