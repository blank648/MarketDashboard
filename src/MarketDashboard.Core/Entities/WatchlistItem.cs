namespace MarketDashboard.Core.Entities;

public class WatchlistItem : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int SymbolId { get; set; }
    public Symbol? SymbolNavigation { get; set; } = null!;
}
