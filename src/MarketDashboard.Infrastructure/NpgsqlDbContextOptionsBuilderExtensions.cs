using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace MarketDashboard.Infrastructure;

public static class NpgsqlDbContextOptionsBuilderExtensions
{
    public static NpgsqlDbContextOptionsBuilder UseTimestampTzDateTimeKind(this NpgsqlDbContextOptionsBuilder builder)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        return builder;
    }
}
