using System;

namespace MarketDashboard.Core.DTOs;

// Response DTOs
public record SymbolDto(
    int Id,
    string Ticker,
    string CompanyName,
    bool IsActive,
    DateTime CreatedAt
);

public record MarketPriceDto(
    int Id,
    string Symbol,
    decimal Price,
    long Volume,
    DateTime RecordedAt
);

public record OhlcvDto(
    int Id,
    string Symbol,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    DateTime PeriodStart,
    DateTime PeriodEnd
);

public record WatchlistItemDto(
    int Id,
    string Symbol,
    string CompanyName,
    DateTime AddedAt
);

public record PriceAlertDto(
    int Id,
    string Symbol,
    decimal ThresholdPrice,
    int Direction,
    bool IsActive,
    DateTime CreatedAt
);

// Request DTOs
public record AddSymbolRequest(
    string Ticker,
    string CompanyName
);

public record AddWatchlistRequest(
    string Ticker
);

public record CreateAlertRequest(
    string Symbol,
    decimal ThresholdPrice,
    int Direction    // 1 = Above, 2 = Below
);

public record UpdateAlertRequest(
    decimal ThresholdPrice,
    int Direction,
    bool IsActive
);
