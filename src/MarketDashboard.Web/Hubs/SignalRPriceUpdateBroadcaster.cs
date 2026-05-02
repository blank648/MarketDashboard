using MarketDashboard.Core.DTOs;
using MarketDashboard.Infrastructure.Workers;
using Microsoft.AspNetCore.SignalR;

namespace MarketDashboard.Web.Hubs;

public class SignalRPriceUpdateBroadcaster(IHubContext<MarketHub> hubContext) : IPriceUpdateBroadcaster
{
    public async Task BroadcastPriceUpdateAsync(string symbol, string companyName,
        decimal currentPrice, decimal? previousPrice,
        long volume, DateTime lastUpdated, CancellationToken ct = default)
    {
        var snapshot = new MarketSnapshotDto(
            symbol, companyName, currentPrice, previousPrice, volume, lastUpdated);

        // Broadcast to symbol-specific group
        await hubContext.Clients
            .Group(MarketHub.GetGroupName(symbol))
            .SendAsync("ReceivePriceUpdate", snapshot, ct);

        // Also broadcast to dashboard group (all-symbols view)
        await hubContext.Clients
            .Group("dashboard")
            .SendAsync("ReceivePriceUpdate", snapshot, ct);
    }
}
