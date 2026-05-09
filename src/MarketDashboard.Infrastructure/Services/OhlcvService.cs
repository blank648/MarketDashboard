using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.Services;

public class OhlcvService : IOhlcvService
{
    private readonly IOhlcvRepository _repository;
    private readonly IMarketDataSource _dataSource;
    private readonly ILogger<OhlcvService> _logger;

    public OhlcvService(
        IOhlcvRepository repository,
        IMarketDataSource dataSource,
        ILogger<OhlcvService> logger)
    {
        _repository = repository;
        _dataSource = dataSource;
        _logger     = logger;
    }

    public async Task SyncAsync(string symbol, CancellationToken ct)
    {
        var lastRecord = await _repository.GetLatestBySymbolAsync(symbol, ct);

        if (lastRecord != null &&
            lastRecord.PeriodStart >= DateTime.UtcNow.AddHours(-24))
        {
            _logger.LogInformation("OHLCV sync skipped for {Symbol} — data is fresh", symbol);
            return;
        }

        var dailyData = await _dataSource.GetDailyOhlcvAsync(symbol, 30, ct);
        int count = 0;

        // Best effort to preserve SymbolId without querying Symbols table
        int symbolId = lastRecord?.SymbolId ?? 0;

        foreach (var data in dailyData)
        {
            var existingRecord = await _repository.FirstOrDefaultAsync(
                r => r.Symbol == symbol && r.PeriodStart == data.PeriodStart, ct);

            if (existingRecord == null)
            {
                var newRecord = new OhlcvRecord
                {
                    Symbol = symbol,
                    Open = data.Open,
                    High = data.High,
                    Low = data.Low,
                    Close = data.Close,
                    Volume = data.Volume,
                    PeriodStart = data.PeriodStart,
                    PeriodEnd = data.PeriodEnd,
                    SymbolId = symbolId
                };
                await _repository.AddAsync(newRecord, ct);
            }
            else
            {
                existingRecord.Open = data.Open;
                existingRecord.High = data.High;
                existingRecord.Low = data.Low;
                existingRecord.Close = data.Close;
                existingRecord.Volume = data.Volume;
                existingRecord.PeriodEnd = data.PeriodEnd;
                _repository.Update(existingRecord);
            }
            count++;
        }

        await _repository.SaveChangesAsync(ct);
        _logger.LogInformation("OHLCV sync complete for {Symbol}: {Count} records", symbol, count);
    }

    public async Task<IEnumerable<OhlcvRecord>> GetRecordsAsync(string symbol, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var toDate = to.ToDateTime(TimeOnly.MaxValue);

        return await _repository.GetBySymbolAsync(symbol, fromDate, toDate, ct);
    }

    public async Task<IEnumerable<string>> GetAvailableSymbolsAsync(CancellationToken ct)
    {
        return await _repository.GetAvailableSymbolsAsync(ct);
    }
}
