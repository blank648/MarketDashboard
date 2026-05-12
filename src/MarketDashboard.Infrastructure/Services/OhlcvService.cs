using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.Services;

public class OhlcvService : IOhlcvService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IMarketDataSource _dataSource;
    private readonly ILogger<OhlcvService> _logger;

    public OhlcvService(
        IDbContextFactory<AppDbContext> dbFactory,
        IMarketDataSource dataSource,
        ILogger<OhlcvService> logger)
    {
        _dbFactory  = dbFactory;
        _dataSource = dataSource;
        _logger     = logger;
    }

    public async Task SyncAsync(string symbol, CancellationToken ct)
    {
        // Single context for the entire unit-of-work (reads + writes must share one context)
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

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

        var dailyData = await _dataSource.GetDailyOhlcvAsync(symbol, 30, ct);
        int count = 0;
        int symbolId = lastRecord?.SymbolId ?? 0;

        foreach (var data in dailyData)
        {
            var existing = await db.OhlcvRecords
                .FirstOrDefaultAsync(
                    r => r.Symbol == symbol && r.PeriodStart == data.PeriodStart, ct);

            if (existing == null)
            {
                db.OhlcvRecords.Add(new OhlcvRecord
                {
                    Symbol      = symbol,
                    Open        = data.Open,
                    High        = data.High,
                    Low         = data.Low,
                    Close       = data.Close,
                    Volume      = data.Volume,
                    PeriodStart = data.PeriodStart,
                    PeriodEnd   = data.PeriodEnd,
                    SymbolId    = symbolId
                });
            }
            else
            {
                existing.Open      = data.Open;
                existing.High      = data.High;
                existing.Low       = data.Low;
                existing.Close     = data.Close;
                existing.Volume    = data.Volume;
                existing.PeriodEnd = data.PeriodEnd;
            }
            count++;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("OHLCV sync complete for {Symbol}: {Count} records", symbol, count);
    }

    public async Task<IEnumerable<OhlcvRecord>> GetRecordsAsync(
        string symbol, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var toDate   = to.ToDateTime(TimeOnly.MaxValue);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.OhlcvRecords
            .Where(r => r.Symbol == symbol
                     && r.PeriodStart >= fromDate
                     && r.PeriodStart <= toDate)
            .OrderBy(r => r.PeriodStart)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetAvailableSymbolsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.OhlcvRecords
            .Select(r => r.Symbol)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);
    }
}
