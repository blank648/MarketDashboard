using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace MarketDashboard.Infrastructure.Services;
public class WatchlistService(AppDbContext db, ILogger<WatchlistService> logger) : IWatchlistService
{
    public async Task<IEnumerable<WatchlistItem>> GetUserWatchlistAsync(string userId, CancellationToken ct)
    {
        return await db.WatchlistItems
            .Where(w => w.UserId == userId)
            .Include(w => w.SymbolNavigation)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(ct);
    }
    public async Task<WatchlistItem> AddToWatchlistAsync(string userId, string symbol, CancellationToken ct)
    {
        symbol = symbol?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));
        }
        var exists = await db.WatchlistItems
            .AnyAsync(w => w.UserId == userId && w.Symbol == symbol, ct);
        if (exists)
        {
            throw new InvalidOperationException($"{symbol} is already on your watchlist.");
        }
        var symbolEntity = await db.Symbols
            .FirstOrDefaultAsync(s => s.Ticker == symbol, ct);
        if (symbolEntity is null)
        {
            symbolEntity = new Symbol 
            { 
                Ticker = symbol, 
                CompanyName = symbol, 
                IsActive = true 
            };
            db.Symbols.Add(symbolEntity);
            await db.SaveChangesAsync(ct);
        }
        var item = new WatchlistItem
        {
            UserId = userId,
            Symbol = symbol,
            AddedAt = DateTime.UtcNow,
            SymbolId = symbolEntity.Id
        };
        db.WatchlistItems.Add(item);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} added {Symbol} to watchlist", userId, symbol);
        return item;
    }
    public async Task RemoveFromWatchlistAsync(string userId, string symbol, CancellationToken ct)
    {
        var item = await db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Symbol == symbol, ct);
        if (item is null)
        {
            return;
        }
        db.WatchlistItems.Remove(item);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} removed {Symbol} from watchlist", userId, symbol);
    }
    public async Task<bool> IsOnWatchlistAsync(string userId, string symbol, CancellationToken ct)
    {
        return await db.WatchlistItems
            .AnyAsync(w => w.UserId == userId && w.Symbol == symbol, ct);
    }
}
