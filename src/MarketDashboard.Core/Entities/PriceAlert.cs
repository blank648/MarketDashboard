using MarketDashboard.Core.Enums;

namespace MarketDashboard.Core.Entities;

public class PriceAlert : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal ThresholdPrice { get; set; }
    public AlertDirection Direction { get; set; }
    public bool IsActive { get; set; } = true;

    public int SymbolId { get; set; }
    public Symbol? SymbolNavigation { get; set; } = null!;

    public ICollection<AlertHistory> History { get; set; } = new List<AlertHistory>();
}
