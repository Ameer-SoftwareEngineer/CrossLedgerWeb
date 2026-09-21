using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Otc;

public sealed record RejectOtcRequestCommand(OtcRequestId OtcRequestId, UserId CallerId) : IRequest;
