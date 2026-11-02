using CrossLedgerWeb.Domain.ValueObjects;
using MediatR;

namespace CrossLedgerWeb.Application.Admin;

public sealed record ApproveCardRequestCommand(CardId CardId) : IRequest;
