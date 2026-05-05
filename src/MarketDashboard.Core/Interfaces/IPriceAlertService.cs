namespace MarketDashboard.Core.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Enums;
public interface IPriceAlertService
{
    Task<IEnumerable<PriceAlert>> GetUserAlertsAsync(
        string userId, CancellationToken ct);
    Task<PriceAlert> CreateAlertAsync(
        string userId, string symbol,
        decimal thresholdPrice, AlertDirection direction,
        CancellationToken ct);
    Task DeleteAlertAsync(int alertId, string userId, CancellationToken ct);
    Task<IEnumerable<AlertHistory>> GetUserAlertHistoryAsync(
        string userId, int limit, CancellationToken ct);
    // Called by polling worker after each price save
    Task CheckAndTriggerAlertsAsync(
        string symbol, decimal currentPrice, CancellationToken ct);
}
