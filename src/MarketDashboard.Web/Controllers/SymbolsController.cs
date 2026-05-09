using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketDashboard.Core.DTOs;
using MarketDashboard.Infrastructure.Data;
using MarketDashboard.Infrastructure.Services;

namespace MarketDashboard.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SymbolsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AdminService _adminService;

    public SymbolsController(
        AppDbContext db,
        AdminService adminService)
    {
        _db = db;
        _adminService = adminService;
    }

    // GET /api/symbols
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SymbolDto>>> GetAll(
        CancellationToken ct)
    {
        var symbols = await _db.Symbols
            .OrderBy(s => s.Ticker)
            .Select(s => new SymbolDto(
                s.Id, s.Ticker, s.CompanyName,
                s.IsActive, s.CreatedAt))
            .ToListAsync(ct);
        return Ok(symbols);
    }

    // GET /api/symbols/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<SymbolDto>> GetById(
        int id, CancellationToken ct)
    {
        var symbol = await _db.Symbols.FindAsync(
            new object[] { id }, ct);
        if (symbol is null)
            throw new KeyNotFoundException(
                $"Symbol with id {id} not found.");
        return Ok(new SymbolDto(
            symbol.Id, symbol.Ticker, symbol.CompanyName,
            symbol.IsActive, symbol.CreatedAt));
    }

    // POST /api/symbols  [Admin only]
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SymbolDto>> Create(
        [FromBody] AddSymbolRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
            throw new ArgumentException("Ticker is required.");

        await _adminService.AddSymbolAsync(
            request.Ticker, request.CompanyName, ct);

        var created = await _db.Symbols
            .FirstAsync(s => s.Ticker ==
                request.Ticker.ToUpper(), ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            new SymbolDto(created.Id, created.Ticker,
                created.CompanyName, created.IsActive,
                created.CreatedAt));
    }

    // PUT /api/symbols/{id}  [Admin only]
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] AddSymbolRequest request,
        CancellationToken ct)
    {
        var symbol = await _db.Symbols.FindAsync(
            new object[] { id }, ct);
        if (symbol is null)
            throw new KeyNotFoundException(
                $"Symbol with id {id} not found.");

        symbol.CompanyName = request.CompanyName;
        symbol.UpdatedAt   = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/symbols/{id}  [Admin only]
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(
        int id, CancellationToken ct)
    {
        var symbol = await _db.Symbols.FindAsync(
            new object[] { id }, ct);
        if (symbol is null)
            throw new KeyNotFoundException(
                $"Symbol with id {id} not found.");

        symbol.IsActive  = false;
        symbol.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
