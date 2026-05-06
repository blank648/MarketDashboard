namespace MarketDashboard.Core.Interfaces;

public interface IOhlcvService
{
    // Fetches from Alpha Vantage if data older than 24h, else returns cached
    Task SyncAsync(string symbol, CancellationToken ct);

    Task<IEnumerable<Entities.OhlcvRecord>> GetRecordsAsync(
        string symbol,
        DateOnly from,
        DateOnly to,
        CancellationToken ct);

    Task<IEnumerable<string>> GetAvailableSymbolsAsync(
        CancellationToken ct);
}

