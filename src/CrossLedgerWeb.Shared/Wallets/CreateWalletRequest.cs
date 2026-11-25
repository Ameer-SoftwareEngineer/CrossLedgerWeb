namespace CrossLedgerWeb.Shared.Wallets;

/// <summary>Wire contract for POST /api/v1/wallets. Deliberately plain primitives, not
/// Domain value objects - CrossLedgerWeb.Shared is referenced by the Blazor client too,
/// which has no business knowing about WalletId or Currency. No OwnerId field - the
/// owner is always the authenticated caller, derived server-side from the bearer token,
/// never client-supplied (a client-supplied OwnerId would let anyone create a wallet
/// nominally "owned" by an arbitrary user id).</summary>
public sealed record CreateWalletRequest(string Currency);
