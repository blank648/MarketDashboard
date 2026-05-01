namespace MarketDashboard.Core.DTOs;

public record OhlcvDto(
    string Symbol,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    DateTime PeriodStart,
    DateTime PeriodEnd
);
