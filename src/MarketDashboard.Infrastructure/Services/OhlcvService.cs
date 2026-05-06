using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.Services;

public class OhlcvService : IOhlcvService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IMarketDataSource _marketDataSource;
    private readonly ILogger<OhlcvService> _logger;

    public OhlcvService(
        IDbContextFactory<AppDbContext> dbFactory,
        IMarketDataSource marketDataSource,
        ILogger<OhlcvService> logger)
    {
        _dbFactory = dbFactory;
        _marketDataSource = marketDataSource;
        _logger = logger;
    }

    public async Task SyncAsync(string symbol, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateDbContextAsync(ct);

        var lastRecord = await db.OhlcvRecords
            .Where(r => r.Symbol == symbol)
            .OrderByDescending(r => r.PeriodStart)
            .FirstOrDefaultAsync(ct);

        if (lastRecord != null &&
            lastRecord.PeriodStart >= DateTime.UtcNow.AddHours(-24))
        {
            _logger.LogInformation("OHLCV sync skipped for {Symbol} — data is fresh", symbol);
            return;
        }

        var dailyData = await _marketDataSource.GetDailyOhlcvAsync(symbol, 30, ct);
        int count = 0;

        var existingSymbol = await db.Symbols.FirstOrDefaultAsync(s => s.Ticker == symbol, ct);
        int symbolId = existingSymbol?.Id ?? 0; // Requires symbol existing, or map differently depending on db.

        foreach (var data in dailyData)
        {
            var existingRecord = await db.OhlcvRecords.FirstOrDefaultAsync(
                r => r.Symbol == symbol && r.PeriodStart == data.PeriodStart, ct);

            if (existingRecord == null)
            {
                db.OhlcvRecords.Add(new OhlcvRecord
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
                });
            }
            else
            {
                existingRecord.Open = data.Open;
                existingRecord.High = data.High;
                existingRecord.Low = data.Low;
                existingRecord.Close = data.Close;
                existingRecord.Volume = data.Volume;
                existingRecord.PeriodEnd = data.PeriodEnd;
            }
            count++;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("OHLCV sync complete for {Symbol}: {Count} records", symbol, count);
    }

    public async Task<IEnumerable<OhlcvRecord>> GetRecordsAsync(string symbol, DateOnly from, DateOnly to, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateDbContextAsync(ct);
        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var toDate = to.ToDateTime(TimeOnly.MaxValue);

        return await db.OhlcvRecords
            .Where(r => r.Symbol == symbol && r.PeriodStart >= fromDate && r.PeriodStart <= toDate)
            .OrderBy(r => r.PeriodStart)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetAvailableSymbolsAsync(CancellationToken ct)
    {
        using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        var ohlcvSymbols = await db.OhlcvRecords
            .Select(r => r.Symbol)
            .Distinct()
            .ToListAsync(ct);

        var activeSymbols = await db.Symbols
            .Where(s => s.IsActive)
            .Select(s => s.Ticker)
            .ToListAsync(ct);

        return ohlcvSymbols.Union(activeSymbols).Order().ToList();
    }
}

