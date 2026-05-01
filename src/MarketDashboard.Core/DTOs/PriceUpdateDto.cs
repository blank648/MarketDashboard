using MarketDashboard.Core.Enums;

namespace MarketDashboard.Core.DTOs;

public record PriceUpdateDto(
    string Symbol,
    decimal Price,
    long Volume,
    DateTime RecordedAt,
    DataSourceProvider Source
);
