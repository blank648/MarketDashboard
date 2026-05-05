using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Enums;
using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.Services;

public class PriceAlertService(
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<PriceAlertService> logger) : IPriceAlertService
{
    public async Task<IEnumerable<PriceAlert>> GetUserAlertsAsync(
        string userId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PriceAlerts
            .Include(a => a.SymbolNavigation)
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PriceAlert> CreateAlertAsync(
        string userId, string symbol,
        decimal thresholdPrice, AlertDirection direction,
        CancellationToken ct)
    {
        if (thresholdPrice <= 0)
            throw new InvalidOperationException("Target price must be greater than 0.");
            
        if (string.IsNullOrWhiteSpace(symbol))
            throw new InvalidOperationException("Symbol cannot be empty.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var sym = await db.Symbols.FirstOrDefaultAsync(s => s.Ticker == symbol, ct);
        if (sym == null)
            throw new InvalidOperationException($"{symbol} is not tracked. Add it to your watchlist first.");

        var alert = new PriceAlert
        {
            UserId = userId,
            Symbol = symbol,
            SymbolId = sym.Id,
            ThresholdPrice = thresholdPrice,
            Direction = direction,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.PriceAlerts.Add(alert);
        await db.SaveChangesAsync(ct);
        return alert;
    }

    public async Task DeleteAlertAsync(int alertId, string userId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var alert = await db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId, ct);
        if (alert == null)
            throw new InvalidOperationException("Alert not found.");

        db.PriceAlerts.Remove(alert);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<AlertHistory>> GetUserAlertHistoryAsync(
        string userId, int limit, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.AlertHistory
            .Include(h => h.Alert) // In case we need alert details
            .Where(h => h.Alert != null && h.Alert.UserId == userId)
            .OrderByDescending(h => h.TriggeredAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task CheckAndTriggerAlertsAsync(
        string symbol, decimal currentPrice, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var alerts = await db.PriceAlerts
            .Where(a => a.Symbol == symbol && a.IsActive)
            .ToListAsync(ct);

        bool changesMade = false;
        foreach (var alert in alerts)
        {
            bool triggered = alert.Direction == AlertDirection.Above
                ? currentPrice >= alert.ThresholdPrice
                : currentPrice <= alert.ThresholdPrice;

            if (triggered)
            {
                alert.IsActive = false; // Mark as triggered
                alert.UpdatedAt = DateTime.UtcNow;

                db.AlertHistory.Add(new AlertHistory
                {
                    AlertId = alert.Id,
                    PriceAtTrigger = currentPrice,
                    TriggeredAt = DateTime.UtcNow
                });
                
                changesMade = true;

                logger.LogInformation(
                    "Alert triggered: {Symbol} {Direction} {Target} @ {Current}",
                    symbol, alert.Direction, alert.ThresholdPrice, currentPrice);
            }
        }

        if (changesMade)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}

