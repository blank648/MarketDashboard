using System.Globalization;
using System.Text.Json;
using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Enums;
using MarketDashboard.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketDashboard.Infrastructure.DataSources;

public class AlphaVantageDataSource(
    IHttpClientFactory httpClientFactory,
    IOptions<AlphaVantageOptions> options,
    ILogger<AlphaVantageDataSource> logger) : IMarketDataSource
{
    private readonly AlphaVantageOptions _options = options.Value;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private const int MinRequestIntervalMs = 12000; // 12 seconds for 5 req/min

    public DataSourceProvider ProviderType => DataSourceProvider.AlphaVantage;

    public async Task<PriceUpdateDto?> GetLatestPriceAsync(string symbol, CancellationToken ct = default)
    {
        try
        {
            await ThrottleAsync(ct);

            var client = httpClientFactory.CreateClient("AlphaVantage");
            var url = $"{_options.BaseUrl}?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(symbol)}&apikey={_options.ApiKey}";

            var response = await client.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.TryGetProperty("Note", out var note))
            {
                logger.LogWarning("Alpha Vantage rate limit reached: {Message}", note.GetString());
                return null;
            }

            if (root.TryGetProperty("Information", out var info))
            {
                logger.LogWarning("Alpha Vantage rate limit reached: {Message}", info.GetString());
                return null;
            }

            if (!root.TryGetProperty("Global Quote", out var quote) || quote.ValueKind == JsonValueKind.Object && !quote.EnumerateObject().Any())
            {
                logger.LogWarning("Alpha Vantage returned no data for symbol: {Symbol}", symbol);
                return null;
            }

            var priceStr = quote.GetProperty("05. price").GetString();
            var volumeStr = quote.GetProperty("06. volume").GetString();
            var dateStr = quote.GetProperty("07. latest trading day").GetString();

            if (string.IsNullOrEmpty(priceStr) || string.IsNullOrEmpty(volumeStr) || string.IsNullOrEmpty(dateStr))
            {
                logger.LogWarning("Alpha Vantage returned incomplete data for symbol: {Symbol}", symbol);
                return null;
            }

            var price = decimal.Parse(priceStr, CultureInfo.InvariantCulture);
            var volume = long.Parse(volumeStr, CultureInfo.InvariantCulture);
            var tradingDay = DateTime.Parse(dateStr, CultureInfo.InvariantCulture);

            return new PriceUpdateDto(symbol.ToUpperInvariant(), price, volume, tradingDay, ProviderType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch price for {Symbol}: {Message}", symbol, ex.Message);
            return null;
        }
    }

    public async Task<IEnumerable<OhlcvDto>> GetDailyOhlcvAsync(string symbol, int days, CancellationToken ct = default)
    {
        try
        {
            await ThrottleAsync(ct);

            var client = httpClientFactory.CreateClient("AlphaVantage");
            var url = $"{_options.BaseUrl}?function=TIME_SERIES_DAILY&symbol={Uri.EscapeDataString(symbol)}&outputsize=compact&apikey={_options.ApiKey}";

            var response = await client.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.TryGetProperty("Note", out var note))
            {
                logger.LogWarning("Alpha Vantage rate limit reached: {Message}", note.GetString());
                return [];
            }

            if (!root.TryGetProperty("Time Series (Daily)", out var timeSeries))
            {
                logger.LogWarning("Alpha Vantage returned no history for symbol: {Symbol}", symbol);
                return [];
            }

            var results = new List<OhlcvDto>();
            foreach (var entry in timeSeries.EnumerateObject().Take(days))
            {
                var date = DateTime.Parse(entry.Name, CultureInfo.InvariantCulture);
                var data = entry.Value;

                var open = decimal.Parse(data.GetProperty("1. open").GetString()!, CultureInfo.InvariantCulture);
                var high = decimal.Parse(data.GetProperty("2. high").GetString()!, CultureInfo.InvariantCulture);
                var low = decimal.Parse(data.GetProperty("3. low").GetString()!, CultureInfo.InvariantCulture);
                var close = decimal.Parse(data.GetProperty("4. close").GetString()!, CultureInfo.InvariantCulture);
                var volume = long.Parse(data.GetProperty("5. volume").GetString()!, CultureInfo.InvariantCulture);

                results.Add(new OhlcvDto(
                    symbol.ToUpperInvariant(),
                    open,
                    high,
                    low,
                    close,
                    volume,
                    date.ToUniversalTime(),
                    date.AddDays(1).AddSeconds(-1).ToUniversalTime()
                ));
            }

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch daily OHLCV for {Symbol}: {Message}", symbol, ex.Message);
            return [];
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await GetLatestPriceAsync("IBM", ct);
            return result != null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Alpha Vantage availability check failed: {Message}", ex.Message);
            return false;
        }
    }

    private static async Task ThrottleAsync(CancellationToken ct)
    {
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequestTime;
            if (elapsed < TimeSpan.FromMilliseconds(MinRequestIntervalMs))
            {
                var delay = MinRequestIntervalMs - (int)elapsed.TotalMilliseconds;
                await Task.Delay(delay, ct);
            }
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}
