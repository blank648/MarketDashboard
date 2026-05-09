using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketDashboard.Core.DTOs;
using MarketDashboard.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDashboard.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PricesController(AppDbContext db) => _db = db;

    // GET /api/prices?symbol=IBM&limit=30
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarketPriceDto>>> GetPrices(
        [FromQuery] string symbol,
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required.");

        var prices = await _db.MarketPrices
            .Where(p => p.Symbol == symbol.ToUpper())
            .OrderByDescending(p => p.RecordedAt)
            .Take(limit)
            .Select(p => new MarketPriceDto(
                p.Id, p.Symbol, p.Price,
                p.Volume, p.RecordedAt))
            .ToListAsync(ct);

        return Ok(prices);
    }

    // GET /api/prices/latest
    [HttpGet("latest")]
    public async Task<ActionResult<IEnumerable<MarketPriceDto>>> GetLatest(
        CancellationToken ct)
    {
        var symbols = await _db.Symbols
            .Where(s => s.IsActive)
            .Select(s => s.Ticker)
            .ToListAsync(ct);

        var result = new List<MarketPriceDto>();
        foreach (var ticker in symbols)
        {
            var p = await _db.MarketPrices
                .Where(x => x.Symbol == ticker)
                .OrderByDescending(x => x.RecordedAt)
                .Select(x => new MarketPriceDto(
                    x.Id, x.Symbol, x.Price,
                    x.Volume, x.RecordedAt))
                .FirstOrDefaultAsync(ct);
            if (p is not null) result.Add(p);
        }
        return Ok(result);
    }

    // GET /api/prices/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MarketPriceDto>> GetById(
        int id, CancellationToken ct)
    {
        var price = await _db.MarketPrices.FindAsync(
            new object[] { id }, ct);
        if (price is null)
            throw new KeyNotFoundException(
                $"Price record {id} not found.");

        return Ok(new MarketPriceDto(
            price.Id, price.Symbol, price.Price,
            price.Volume, price.RecordedAt));
    }
}
