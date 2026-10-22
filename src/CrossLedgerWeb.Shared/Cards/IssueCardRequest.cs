namespace CrossLedgerWeb.Shared.Cards;

public sealed record IssueCardRequest(Guid LinkedWalletId, string CardholderName);
