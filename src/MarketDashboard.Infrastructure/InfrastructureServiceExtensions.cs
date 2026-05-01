using MarketDashboard.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.DataSources;
using Microsoft.Extensions.Options;

namespace MarketDashboard.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connStr, npgsqlOptions =>
                npgsqlOptions.UseTimestampTzDateTimeKind()));

        // Alpha Vantage configuration
        services.Configure<AlphaVantageOptions>(
            configuration.GetSection("AlphaVantage"));

        // Named HttpClient for Alpha Vantage
        services.AddHttpClient("AlphaVantage", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(
                "User-Agent", "MarketDashboard/1.0");
        });

        // Register data source
        services.AddScoped<IMarketDataSource, AlphaVantageDataSource>();

        return services;
    }
}
