namespace MarketDashboard.Core.DTOs;

public record UserAdminDto(
    string Id,
    string Email,
    string? UserName,
    bool IsAdmin
);
