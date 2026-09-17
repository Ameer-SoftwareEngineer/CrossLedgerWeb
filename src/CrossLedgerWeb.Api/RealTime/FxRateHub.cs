using CrossLedgerWeb.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CrossLedgerWeb.Api.RealTime;

/// <summary>Backs the FX Rates screen's live ticker (specification 9). Clients only ever
/// receive - FxRateBroadcastService pushes "RateUpdated" to everyone connected - so there
/// are no hub methods to call.</summary>
[Authorize(Roles = Roles.Customer)]
public sealed class FxRateHub : Hub;
