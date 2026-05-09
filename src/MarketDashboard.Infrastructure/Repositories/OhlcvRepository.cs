using MarketDashboard.Core.Entities;
using MarketDashboard.Core.Interfaces;
using MarketDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketDashboard.Infrastructure.Repositories;

public class OhlcvRepository : Repository<OhlcvRecord>,
    IOhlcvRepository
{
    public OhlcvRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<OhlcvRecord>> GetBySymbolAsync(
        string symbol,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
        => await _set
            .Where(r => r.Symbol == symbol &&
                        r.PeriodStart >= from &&
                        r.PeriodStart <= to)
            .OrderBy(r => r.PeriodStart)
            .ToListAsync(ct);

    public async Task<IEnumerable<string>> GetAvailableSymbolsAsync(
        CancellationToken ct = default)
        => await _set
            .Select(r => r.Symbol)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);

    public async Task<OhlcvRecord?> GetLatestBySymbolAsync(
        string symbol,
        CancellationToken ct = default)
        => await _set
            .Where(r => r.Symbol == symbol)
            .OrderByDescending(r => r.PeriodStart)
            .FirstOrDefaultAsync(ct);
}
