using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.Data;
using MarketDashboard.Infrastructure.DataSources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketDashboard.Infrastructure.Workers;

public class MarketDataPollingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AlphaVantageOptions> options,
    ILogger<MarketDataPollingWorker> logger,
    IPriceUpdateBroadcaster broadcaster) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = options.Value.PollingInterval;
    private readonly string[] _fallbackSymbols = ["AAPL", "IBM"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MarketDataPollingWorker started. Polling interval: {Interval}", _pollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllSymbolsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Polling cycle failed: {Message}", ex.Message);
            }

            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("MarketDataPollingWorker stopped.");
    }

    private async Task PollAllSymbolsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dataSource = scope.ServiceProvider.GetRequiredService<IMarketDataSource>();

        var symbols = await db.Symbols
            .Where(s => s.IsActive)
            .Select(s => s.Ticker)
            .ToListAsync(ct);

        if (symbols.Count == 0)
        {
            logger.LogWarning("No active symbols in DB, using fallback symbols");
            symbols = [.. _fallbackSymbols];
        }

        logger.LogInformation("Polling {Count} symbols", symbols.Count);

        var priceUpdates = new List<PriceUpdateDto>();

        foreach (var symbol in symbols)
        {
            var dto = await dataSource.GetLatestPriceAsync(symbol, ct);
            if (dto is null)
            {
                logger.LogWarning("No data returned for {Symbol}", symbol);
                continue;
            }

            priceUpdates.Add(dto);

            var symbolId = await GetOrCreateSymbolIdAsync(db, dto.Symbol, ct);

            var price = new MarketPrice
            {
                Symbol = dto.Symbol,
                Price = dto.Price,
                Volume = dto.Volume,
                RecordedAt = dto.RecordedAt.Kind == DateTimeKind.Utc 
                    ? dto.RecordedAt 
                    : dto.RecordedAt.ToUniversalTime(),
                Source = dto.Source,
                SymbolId = symbolId
            };

            db.MarketPrices.Add(price);
        }

        await db.SaveChangesAsync(ct);
        
        var alertService = scope.ServiceProvider.GetRequiredService<IPriceAlertService>();
        foreach (var update in priceUpdates)
        {
            await alertService.CheckAndTriggerAlertsAsync(update.Symbol, update.Price, ct);
        }

        logger.LogInformation("Polling cycle complete. Saved prices for {Count} symbols", symbols.Count);

        foreach (var update in priceUpdates)
        {
            try
            {
                await broadcaster.BroadcastPriceUpdateAsync(
                    update.Symbol,
                    update.Symbol, // Using ticker as placeholder for company name
                    update.Price,
                    null,          // previousPrice: null for now
                    update.Volume,
                    update.RecordedAt.Kind == DateTimeKind.Utc
                        ? update.RecordedAt
                        : update.RecordedAt.ToUniversalTime(),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to broadcast price for {Symbol}: {Message}", update.Symbol, ex.Message);
            }
        }
    }

    private async Task<int> GetOrCreateSymbolIdAsync(
        AppDbContext db, string ticker, CancellationToken ct)
    {
        var symbol = await db.Symbols
            .FirstOrDefaultAsync(s => s.Ticker == ticker, ct);

        if (symbol is null)
        {
            symbol = new Symbol
            {
                Ticker = ticker,
                CompanyName = ticker,
                IsActive = true
            };
            db.Symbols.Add(symbol);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Auto-created symbol entry for {Ticker}", ticker);
        }

        return symbol.Id;
    }
}
