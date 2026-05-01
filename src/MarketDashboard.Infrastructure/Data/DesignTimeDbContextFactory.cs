using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketDashboard.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("MARKETDASHBOARD_DB") 
                      ?? "Host=localhost;Port=5433;Database=marketdashboard;Username=marketdashboard;Password=marketdashboard";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connStr, npgsqlOptions =>
            npgsqlOptions.UseTimestampTzDateTimeKind());

        return new AppDbContext(optionsBuilder.Options);
    }
}
