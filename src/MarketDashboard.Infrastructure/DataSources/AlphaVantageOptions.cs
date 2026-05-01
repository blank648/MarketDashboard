namespace MarketDashboard.Infrastructure.DataSources;

public class AlphaVantageOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
    public int PollingIntervalSeconds { get; set; } = 60;

    public TimeSpan PollingInterval => TimeSpan.FromSeconds(PollingIntervalSeconds);
}
