namespace CrossLedgerWeb.Application.Auth;

/// <summary>The step-up policy table from specification 6.2 - "Yes" rows only; read-only
/// operations and transfers under the configured threshold never appear here because
/// they're not step-up-gated at all.</summary>
public enum StepUpOperation
{
    HighValueTransfer,
    AddOrEditBeneficiary,
    RevealCardDetails,
    ChangeSpendingLimits,
    DisableTwoFactor,
    ChangePasswordOrEmail,

    /// <summary>At least as sensitive as ChangeSpendingLimits/RevealCardDetails - same
    /// unconditional (no threshold) gating.</summary>
    ChangeCardPin,

    /// <summary>Replacing a card (lost/stolen) closes the old one and issues a new one -
    /// at least as sensitive as changing its spending limits.</summary>
    ReissueCard,
}
