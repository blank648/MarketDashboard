using System.ComponentModel.DataAnnotations;

namespace MarketDashboard.Web.Models;

public class AddSymbolModel
{
    [Required(ErrorMessage = "Ticker symbol is required.")]
    [StringLength(10, MinimumLength = 1,
        ErrorMessage = "Ticker must be 1–10 characters.")]
    [RegularExpression(@"^[A-Za-z0-9]+$",
        ErrorMessage = "Ticker must contain only letters and numbers.")]
    public string Ticker { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Company name must be 2–100 characters.")]
    public string CompanyName { get; set; } = string.Empty;
}

public class CreateAlertModel
{
    [Required(ErrorMessage = "Symbol is required.")]
    [StringLength(10, MinimumLength = 1,
        ErrorMessage = "Symbol must be 1–10 characters.")]
    public string Symbol { get; set; } = string.Empty;

    [Required(ErrorMessage = "Threshold price is required.")]
    [Range(0.01, 1_000_000,
        ErrorMessage = "Price must be between $0.01 and $1,000,000.")]
    public decimal ThresholdPrice { get; set; }

    [Required(ErrorMessage = "Direction is required.")]
    [Range(1, 2,
        ErrorMessage = "Direction must be Above (1) or Below (2).")]
    public int Direction { get; set; } = 1;
}

public class AddWatchlistModel
{
    [Required(ErrorMessage = "Symbol is required.")]
    [StringLength(10, MinimumLength = 1,
        ErrorMessage = "Symbol must be 1–10 characters.")]
    [RegularExpression(@"^[A-Za-z0-9]+$",
        ErrorMessage = "Symbol must contain only letters and numbers.")]
    public string Ticker { get; set; } = string.Empty;
}
