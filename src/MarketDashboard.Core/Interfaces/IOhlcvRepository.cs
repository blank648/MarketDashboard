using MarketDashboard.Core.Entities;

namespace MarketDashboard.Core.Interfaces;

/// <summary>
/// Specialized repository for OHLCV time-series queries.
/// Extends IRepository<OhlcvRecord> with domain-specific methods.
/// </summary>
public interface IOhlcvRepository : IRepository<OhlcvRecord>
{
    Task<IEnumerable<OhlcvRecord>> GetBySymbolAsync(
        string symbol,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    Task<IEnumerable<string>> GetAvailableSymbolsAsync(
        CancellationToken ct = default);

    Task<OhlcvRecord?> GetLatestBySymbolAsync(
        string symbol,
        CancellationToken ct = default);
}
