using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Entities;
using MarketDashboard.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketDashboard.Infrastructure.Services;

public class AdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<AdminService> _logger;

    public const string AdminRole = "Admin";

    public AdminService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<AdminService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task EnsureAdminRoleExistsAsync(CancellationToken ct)
    {
        if (!(await _roleManager.RoleExistsAsync(AdminRole)))
        {
            await _roleManager.CreateAsync(new IdentityRole(AdminRole));
            _logger.LogInformation("Admin role ensured");
        }
    }

    public async Task<IEnumerable<UserAdminDto>> GetAllUsersAsync(CancellationToken ct)
    {
        var users = await _userManager.Users.ToListAsync(ct);
        var dtos = new List<UserAdminDto>();

        foreach (var user in users)
        {
            var isAdmin = await _userManager.IsInRoleAsync(user, AdminRole);
            dtos.Add(new UserAdminDto(user.Id, user.Email!, user.UserName, isAdmin));
        }

        return dtos;
    }

    public async Task<string> ToggleAdminRoleAsync(string userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, AdminRole);
        if (isAdmin)
        {
            await _userManager.RemoveFromRoleAsync(user, AdminRole);
            _logger.LogInformation("Admin role toggled for {Email}: removed", user.Email);
            return "Admin role removed";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, AdminRole);
            _logger.LogInformation("Admin role toggled for {Email}: granted", user.Email);
            return "Admin role granted";
        }
    }

    public async Task DeleteUserAsync(string userId, string requestingUserId, CancellationToken ct)
    {
        if (userId == requestingUserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, roles);
        }

        await _userManager.DeleteAsync(user);
        _logger.LogInformation("User {Email} deleted by admin", user.Email);
    }

    public async Task<IEnumerable<Symbol>> GetAllSymbolsAsync(CancellationToken ct)
    {
        using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Symbols
            .OrderBy(s => s.Ticker)
            .ToListAsync(ct);
    }

    public async Task AddSymbolAsync(string ticker, string companyName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ticker) || ticker.Length > 10 || !ticker.All(char.IsLetterOrDigit))
        {
            throw new ArgumentException("Ticker must be 1-10 characters, alphanumeric.");
        }

        ticker = ticker.ToUpperInvariant();

        using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        var exists = await db.Symbols.AnyAsync(s => s.Ticker == ticker, ct);
        if (exists)
        {
            throw new InvalidOperationException("Symbol already exists");
        }

        var symbol = new Symbol
        {
            Ticker = ticker,
            CompanyName = companyName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Symbols.Add(symbol);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Symbol {Ticker} added by admin", ticker);
    }

    public async Task<string> ToggleSymbolActiveAsync(int symbolId, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        var symbol = await db.Symbols.FindAsync(new object[] { symbolId }, ct);
        if (symbol == null)
        {
            throw new InvalidOperationException("Symbol not found");
        }

        symbol.IsActive = !symbol.IsActive;
        symbol.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync(ct);
        
        var status = symbol.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("Symbol {Ticker} {Status}", symbol.Ticker, status);
        
        return status;
    }
}
