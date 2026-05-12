using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Enums;
using MarketDashboard.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.DataSources;

public class HybridMarketDataSource(
    CppSharedDbDataSource cpp,
    AlphaVantageDataSource alphaVantage,
    ILogger<HybridMarketDataSource> logger) : IMarketDataSource
{
    public DataSourceProvider ProviderType => DataSourceProvider.AlphaVantage;

    public async Task<PriceUpdateDto?> GetLatestPriceAsync(string symbol, CancellationToken ct = default)
    {
        if (await cpp.IsAvailableAsync(ct))
        {
            logger.LogDebug("Using CppProcessor data source for {Symbol}", symbol);
            return await cpp.GetLatestPriceAsync(symbol, ct);
        }

        logger.LogDebug("CppProcessor data stale, falling back to AlphaVantage for {Symbol}", symbol);
        return await alphaVantage.GetLatestPriceAsync(symbol, ct);
    }

    public Task<IEnumerable<OhlcvDto>> GetDailyOhlcvAsync(string symbol, int days, CancellationToken ct = default)
    {
        return alphaVantage.GetDailyOhlcvAsync(symbol, days, ct);
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return alphaVantage.IsAvailableAsync(ct);
    }
}
