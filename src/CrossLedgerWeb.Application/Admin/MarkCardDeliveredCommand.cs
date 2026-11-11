using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record MarkCardDeliveredCommand(CardId CardId) : IRequest;
