namespace MarketDashboard.Infrastructure.Workers;

public interface IPriceUpdateBroadcaster
{
    Task BroadcastPriceUpdateAsync(string symbol, string companyName,
        decimal currentPrice, decimal? previousPrice,
        long volume, DateTime lastUpdated, CancellationToken ct = default);
}
