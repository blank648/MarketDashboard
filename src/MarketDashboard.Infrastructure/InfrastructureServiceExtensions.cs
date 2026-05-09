using MarketDashboard.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MarketDashboard.Infrastructure.Workers;

using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.DataSources;
using MarketDashboard.Infrastructure.Services;
using Microsoft.Extensions.Options;
using MarketDashboard.Infrastructure.Repositories;

namespace MarketDashboard.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connStr, npgsqlOptions =>
                npgsqlOptions.UseTimestampTzDateTimeKind()));
            
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

        // Identity configuration
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        // Background polling worker
        services.AddHostedService<MarketDataPollingWorker>();

        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IPriceAlertService, PriceAlertService>();
        services.AddScoped<IOhlcvService, OhlcvService>();
        services.AddScoped<AdminService>();

        // Repository Pattern
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IOhlcvRepository, OhlcvRepository>();

        return services;
    }
}
