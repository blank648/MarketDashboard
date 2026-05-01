namespace MarketDashboard.Core.Entities;

public class AlertHistory : BaseEntity
{
    public int AlertId { get; set; }
    public PriceAlert? Alert { get; set; } = null!;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public decimal PriceAtTrigger { get; set; }
}
