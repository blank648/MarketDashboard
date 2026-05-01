using MarketDashboard.Core.Entities;

namespace MarketDashboard.Core.Interfaces;

public interface ISymbolService
{
    Task<IEnumerable<Symbol>> GetActiveSymbolsAsync(CancellationToken ct = default);
    Task<Symbol?> GetByTickerAsync(string ticker, CancellationToken ct = default);
    Task<Symbol> AddSymbolAsync(string ticker, string companyName, CancellationToken ct = default);
    Task SetSymbolActiveAsync(string ticker, bool isActive, CancellationToken ct = default);
}
