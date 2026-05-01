namespace MarketDashboard.Core.Entities;

public class OhlcvRecord : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public int SymbolId { get; set; }
    public Symbol? SymbolNavigation { get; set; } = null!;
}
