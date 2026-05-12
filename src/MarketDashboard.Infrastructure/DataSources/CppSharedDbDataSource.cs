using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Enums;
using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.DataSources;

public class CppSharedDbDataSource(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<CppSharedDbDataSource> logger) : IMarketDataSource
{
    public const string SourceName = "CppProcessor";

    private TimeSpan StalenessThreshold => TimeSpan.FromMinutes(
        configuration.GetValue<int>("DataSource:CppStalenessThresholdMinutes", 5));

    public DataSourceProvider ProviderType => DataSourceProvider.CppProcessor;

    public async Task<PriceUpdateDto?> GetLatestPriceAsync(string symbol, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - StalenessThreshold;
        var price = await db.MarketPrices
            .Where(p => p.Source == DataSourceProvider.CppProcessor
                        && p.Symbol == symbol.ToUpperInvariant()
                        && p.RecordedAt > cutoff)
            .OrderByDescending(p => p.RecordedAt)
            .FirstOrDefaultAsync(ct);

        if (price is null)
        {
            logger.LogDebug("No fresh CppProcessor data for {Symbol}", symbol);
            return null;
        }

        return new PriceUpdateDto(price.Symbol, price.Price, price.Volume, price.RecordedAt, DataSourceProvider.CppProcessor);
    }

    public Task<IEnumerable<OhlcvDto>> GetDailyOhlcvAsync(string symbol, int days, CancellationToken ct = default)
    {
        // C++ processor writes price ticks only — OHLCV not available from this source
        return Task.FromResult<IEnumerable<OhlcvDto>>([]);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - StalenessThreshold;
        return await db.MarketPrices
            .AnyAsync(p => p.Source == DataSourceProvider.CppProcessor && p.RecordedAt > cutoff, ct);
    }
}
