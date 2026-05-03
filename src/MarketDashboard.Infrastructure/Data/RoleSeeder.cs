using MarketDashboard.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MarketDashboard.Core.Entities;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.Data;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        string[] roles = { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed fallback symbols
        var fallbackSymbols = new[]
        {
            new { Ticker = "AAPL", CompanyName = "Apple Inc." },
            new { Ticker = "IBM",  CompanyName = "International Business Machines" }
        };

        foreach (var s in fallbackSymbols)
        {
            var exists = await db.Symbols
                .AnyAsync(x => x.Ticker == s.Ticker);
            if (!exists)
            {
                db.Symbols.Add(new Symbol
                {
                    Ticker = s.Ticker,
                    CompanyName = s.CompanyName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Fallback symbols seeded: AAPL, IBM");
    }
}
