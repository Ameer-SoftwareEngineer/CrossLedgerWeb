using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

public sealed record IssueCardCommand(UserId OwnerId, WalletId LinkedWalletId, string CardholderName) : IRequest<IssueCardResult>;
