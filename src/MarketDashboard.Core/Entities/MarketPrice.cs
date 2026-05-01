using MarketDashboard.Core.Enums;

namespace MarketDashboard.Core.Entities;

public class MarketPrice : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public long Volume { get; set; }
    public DateTime RecordedAt { get; set; }
    public DataSourceProvider Source { get; set; }
    
    public int SymbolId { get; set; }
    public Symbol? SymbolNavigation { get; set; } = null!;
}
