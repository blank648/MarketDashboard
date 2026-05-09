using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketDashboard.Core.DTOs;
using MarketDashboard.Core.Entities;
using MarketDashboard.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDashboard.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly AppDbContext _db;

    // Helper to get current user ID from claims
    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value ?? string.Empty;

    public WatchlistController(AppDbContext db)
        => _db = db;

    // GET /api/watchlist
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WatchlistItemDto>>> GetAll(
        CancellationToken ct)
    {
        var userId = CurrentUserId;
        var items = await _db.WatchlistItems
            .Include(w => w.SymbolNavigation)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => new WatchlistItemDto(
                w.Id,
                w.Symbol,
                w.SymbolNavigation!.CompanyName,
                w.AddedAt))
            .ToListAsync(ct);
        return Ok(items);
    }

    // POST /api/watchlist
    [HttpPost]
    public async Task<ActionResult<WatchlistItemDto>> Add(
        [FromBody] AddWatchlistRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
            throw new ArgumentException("Ticker is required.");

        var symbol = await _db.Symbols
            .FirstOrDefaultAsync(s =>
                s.Ticker == request.Ticker.ToUpper(), ct);
        if (symbol is null)
            throw new KeyNotFoundException(
                $"Symbol {request.Ticker} not found.");

        var userId = CurrentUserId;
        var exists = await _db.WatchlistItems.AnyAsync(
            w => w.UserId == userId &&
                 w.SymbolId == symbol.Id, ct);
        if (exists)
            throw new ArgumentException(
                "Symbol already in watchlist.");

        var item = new WatchlistItem
        {
            UserId   = userId,
            SymbolId = symbol.Id,
            Symbol   = symbol.Ticker,
            AddedAt  = DateTime.UtcNow
        };
        _db.WatchlistItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetAll), null,
            new WatchlistItemDto(
                item.Id, item.Symbol,
                symbol.CompanyName, item.AddedAt));
    }

    // DELETE /api/watchlist/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(
        int id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        var item = await _db.WatchlistItems
            .FirstOrDefaultAsync(w =>
                w.Id == id && w.UserId == userId, ct);
        if (item is null)
            throw new KeyNotFoundException(
                $"Watchlist item {id} not found.");

        _db.WatchlistItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
