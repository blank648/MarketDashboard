namespace MarketDashboard.Core.DTOs;

public record MarketSnapshotDto(
    string Symbol,
    string CompanyName,
    decimal CurrentPrice,
    decimal? PreviousPrice,
    long Volume,
    DateTime LastUpdated
)
{
    public decimal? ChangePercent => PreviousPrice.HasValue && PreviousPrice.Value != 0
        ? Math.Round(((CurrentPrice - PreviousPrice.Value) / PreviousPrice.Value) * 100, 2)
        : null;
}
