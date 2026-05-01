namespace MarketDashboard.Core.Entities;

public class Symbol : BaseEntity
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<MarketPrice> Prices { get; set; } = new List<MarketPrice>();
    public ICollection<OhlcvRecord> OhlcvRecords { get; set; } = new List<OhlcvRecord>();
    public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
    public ICollection<PriceAlert> PriceAlerts { get; set; } = new List<PriceAlert>();
}
