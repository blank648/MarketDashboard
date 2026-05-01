using MarketDashboard.Core.Entities;

namespace MarketDashboard.Core.Interfaces;

public interface IWatchlistService
{
    Task<IEnumerable<WatchlistItem>> GetUserWatchlistAsync(string userId, CancellationToken ct = default);
    Task<WatchlistItem> AddToWatchlistAsync(string userId, string symbol, CancellationToken ct = default);
    Task RemoveFromWatchlistAsync(string userId, string symbol, CancellationToken ct = default);
    Task<bool> IsOnWatchlistAsync(string userId, string symbol, CancellationToken ct = default);
}
