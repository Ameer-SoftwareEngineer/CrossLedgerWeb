using CrossLedgerWeb.Domain.Cards;
using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Replaces a lost/stolen/damaged card (specification 12.4/19) - closes the old
/// one and issues a brand new one on the SAME CardWalletId, so the balance carries over
/// with no extra bookkeeping. NewShippingAddress lets the customer update where the
/// replacement ships to; null keeps the old card's shipping address.</summary>
public sealed record ReissueCardCommand(CardId OldCardId, UserId CallerId, CardAddress? NewShippingAddress) : IRequest<ReissueCardResult>;
