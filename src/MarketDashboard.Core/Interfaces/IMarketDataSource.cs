using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Enums;

namespace MarketDashboard.Core.Interfaces;

public interface IMarketDataSource
{
    Task<PriceUpdateDto?> GetLatestPriceAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<OhlcvDto>> GetDailyOhlcvAsync(string symbol, int days, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    DataSourceProvider ProviderType { get; }
}
