using Microsoft.AspNetCore.SignalR;

namespace MarketDashboard.Web.Hubs;

public class MarketHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public async Task JoinSymbolGroup(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(symbol));
    }

    public async Task LeaveSymbolGroup(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(symbol));
    }

    public async Task JoinDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
    }

    public async Task LeaveDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
    }

    public static string GetGroupName(string symbol)
    {
        return $"symbol-{symbol.ToUpperInvariant()}";
    }
}
