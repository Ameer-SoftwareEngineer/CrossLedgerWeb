using CrossLedgerWeb.Shared.Auth;

namespace CrossLedgerWeb.Shared.Otc;

/// <summary>Wire contract for POST /api/v1/otc/requests/{id}/accept. Amount is the
/// requested amount the client already received back from the create call - echoed here
/// so [RequireStepUp] can read it off the bound model before the handler (and the
/// database) is reached, same reasoning as CreateTransferRequest.SourceAmount.</summary>
public sealed record AcceptOtcRequestRequest(Guid TargetWalletId, decimal Amount) : IStepUpAmountSource
{
    decimal IStepUpAmountSource.StepUpAmount => Amount;
}
