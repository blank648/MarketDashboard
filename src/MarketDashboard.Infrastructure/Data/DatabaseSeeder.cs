using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            await SeedSymbolsAsync(db, ct);
            await SeedUsersAsync(db, userManager, ct);
            await SeedMarketPricesAsync(db, ct);
            await SeedOhlcvAsync(db, ct);
            await SeedWatchlistAsync(db, userManager, ct);
            await SeedAlertsAsync(db, userManager, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database seeding failed");
            throw;
        }
    }

    private async Task SeedSymbolsAsync(AppDbContext db, CancellationToken ct)
    {
        var symbolsToSeed = new[]
        {
            new Symbol { Ticker = "MSFT", CompanyName = "Microsoft Corporation", IsActive = true },
            new Symbol { Ticker = "GOOGL", CompanyName = "Alphabet Inc.", IsActive = true },
            new Symbol { Ticker = "AMZN", CompanyName = "Amazon.com Inc.", IsActive = true }
        };

        bool added = false;
        foreach (var s in symbolsToSeed)
        {
            if (!await db.Symbols.AnyAsync(x => x.Ticker == s.Ticker, ct))
            {
                db.Symbols.Add(s);
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Symbols seeded");
        }
    }

    private async Task SeedUsersAsync(AppDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync("demo@marketdash.com") == null)
        {
            var user = new ApplicationUser
            {
                UserName = "demo",
                Email = "demo@marketdash.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Demo1234!");
            _logger.LogInformation("Demo user seeded");
        }
    }

    private async Task SeedMarketPricesAsync(AppDbContext db, CancellationToken ct)
    {
        var tickers = new[] { "IBM", "AAPL", "TSLA", "MSFT", "GOOGL", "AMZN" };
        var basePrices = new Dictionary<string, decimal>
        {
            { "IBM", 150m }, { "AAPL", 185m }, { "TSLA", 245m },
            { "MSFT", 415m }, { "GOOGL", 175m }, { "AMZN", 185m }
        };

        int count = 0;
        foreach (var ticker in tickers)
        {
            var symbol = await db.Symbols.FirstAsync(s => s.Ticker == ticker, ct);
            var basePrice = basePrices[ticker];
            bool addedForSymbol = false;

            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var dayDate = date.Date;
                var nextDay = dayDate.AddDays(1);

                // Per-day guard to prevent duplicates while filling missing days
                var exists = await db.MarketPrices.AnyAsync(
                    p => p.Symbol == ticker && p.RecordedAt >= dayDate && p.RecordedAt < nextDay, ct);
                if (exists) continue;

                var random = new Random(ticker.GetHashCode() + i);
                var price = basePrice * (1m + (decimal)(random.NextDouble() - 0.5) * 0.04m);
                var volume = random.NextInt64(500_000, 5_000_000);

                db.MarketPrices.Add(new MarketPrice
                {
                    Symbol = ticker,
                    Price = Math.Round(price, 2),
                    Volume = volume,
                    RecordedAt = date,
                    Source = DataSourceProvider.AlphaVantage,
                    SymbolId = symbol.Id
                });
                addedForSymbol = true;
            }

            if (addedForSymbol)
            {
                await db.SaveChangesAsync(ct);
                count++;
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Market prices seeded for {Count} symbols", count);
        }
    }

    private async Task SeedOhlcvAsync(AppDbContext db, CancellationToken ct)
    {
        var tickers = new[] { "IBM", "AAPL", "TSLA", "MSFT", "GOOGL", "AMZN" };
        var basePrices = new Dictionary<string, decimal>
        {
            { "IBM", 150m }, { "AAPL", 185m }, { "TSLA", 245m },
            { "MSFT", 415m }, { "GOOGL", 175m }, { "AMZN", 185m }
        };

        int count = 0;
        foreach (var ticker in tickers)
        {
            var symbol = await db.Symbols.FirstAsync(s => s.Ticker == ticker, ct);
            var basePrice = basePrices[ticker];
            bool addedForSymbol = false;

            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var periodStart = date.Date;
                var periodEnd = periodStart.AddHours(23).AddMinutes(59);

                // Per-day guard to avoid unique constraint violations
                if (await db.OhlcvRecords.AnyAsync(o => o.Symbol == ticker && o.PeriodStart == periodStart, ct))
                    continue;

                var random = new Random(ticker.GetHashCode() + i + 1000);
                var open = basePrice * (1m + (decimal)(random.NextDouble() - 0.5) * 0.03m);
                var close = open * (1m + (decimal)(random.NextDouble() - 0.5) * 0.02m);
                var high = Math.Max(open, close) * (1m + (decimal)random.NextDouble() * 0.01m);
                var low = Math.Min(open, close) * (1m - (decimal)random.NextDouble() * 0.01m);
                var volume = random.NextInt64(1_000_000, 10_000_000);

                db.OhlcvRecords.Add(new OhlcvRecord
                {
                    Symbol = ticker,
                    Open = Math.Round(open, 2),
                    High = Math.Round(high, 2),
                    Low = Math.Round(low, 2),
                    Close = Math.Round(close, 2),
                    Volume = volume,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    SymbolId = symbol.Id
                });
                addedForSymbol = true;
            }

            if (addedForSymbol)
            {
                await db.SaveChangesAsync(ct);
                count++;
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("OHLCV data seeded for {Count} symbols", count);
        }
    }

    private async Task SeedWatchlistAsync(AppDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync("test1234@test.com");
        if (user == null) return;

        var tickers = new[] { "AAPL", "MSFT", "TSLA" };
        bool added = false;

        foreach (var ticker in tickers)
        {
            var symbol = await db.Symbols.FirstOrDefaultAsync(s => s.Ticker == ticker, ct);
            if (symbol == null) continue;

            var exists = await db.WatchlistItems.AnyAsync(
                w => w.UserId == user.Id && w.SymbolId == symbol.Id, ct);
            if (!exists)
            {
                db.WatchlistItems.Add(new WatchlistItem
                {
                    UserId = user.Id,
                    SymbolId = symbol.Id,
                    Symbol = ticker,
                    AddedAt = DateTime.UtcNow
                });
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Watchlist seeded for demo user");
        }
    }

    private async Task SeedAlertsAsync(AppDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync("test1234@test.com");
        if (user == null) return;

        if (await db.PriceAlerts.AnyAsync(a => a.UserId == user.Id, ct)) return;

        var alerts = new[]
        {
            new { Ticker = "AAPL", Threshold = 200m, Direction = AlertDirection.Above },
            new { Ticker = "TSLA", Threshold = 200m, Direction = AlertDirection.Below },
            new { Ticker = "MSFT", Threshold = 450m, Direction = AlertDirection.Above }
        };

        foreach (var a in alerts)
        {
            var symbol = await db.Symbols.FirstOrDefaultAsync(s => s.Ticker == a.Ticker, ct);
            if (symbol == null) continue;

            db.PriceAlerts.Add(new PriceAlert
            {
                UserId = user.Id,
                SymbolId = symbol.Id,
                Symbol = a.Ticker,
                ThresholdPrice = a.Threshold,
                Direction = a.Direction,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Demo alerts seeded");
    }
}
