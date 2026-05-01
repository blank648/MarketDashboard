using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Enums;

namespace MarketDashboard.Core.Interfaces;

public interface IAlertService
{
    Task<IEnumerable<PriceAlert>> GetUserAlertsAsync(string userId, CancellationToken ct = default);
    Task<PriceAlert> CreateAlertAsync(string userId, string symbol, decimal threshold, AlertDirection direction, CancellationToken ct = default);
    Task DeactivateAlertAsync(int alertId, string userId, CancellationToken ct = default);
    Task<IEnumerable<PriceAlert>> GetActiveAlertsForSymbolAsync(string symbol, CancellationToken ct = default);
    Task RecordAlertTriggeredAsync(int alertId, decimal priceAtTrigger, CancellationToken ct = default);
}
