using CrossLedgerWeb.Shared.Auth;

namespace CrossLedgerWeb.Shared.Cards;

public sealed record TopUpCardRequest(Guid SourceWalletId, decimal Amount) : IStepUpAmountSource
{
    decimal IStepUpAmountSource.StepUpAmount => Amount;
}
