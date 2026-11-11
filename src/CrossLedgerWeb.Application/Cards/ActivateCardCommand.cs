using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Cards;

/// <summary>Customer-only (not admin) - confirming receipt of your own card isn't
/// sensitive the way changing a PIN or spending limit is, so unlike those, this is not
/// step-up gated (same risk tier as Freeze/Unfreeze).</summary>
public sealed record ActivateCardCommand(CardId CardId, UserId CallerId) : IRequest;
